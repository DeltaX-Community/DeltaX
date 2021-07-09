namespace DeltaX.Modules.RealTimeRpcWebSocket
{
    using DeltaX.Connections.WebSocket;
    using DeltaX.RealTime;
    using DeltaX.RealTime.Interfaces;
    using DeltaX.RealTime.RtExpression;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using DeltaX.Rpc.JsonRpc.WebSocketConnection;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;


    public class RealTimeRpcWebSocketMiddleware : RpcWebSocketMiddleware, IRpcWebSocketMiddleware
    {
        private ILogger logger;
        private IRtConnector connector;
        private WebSocketHandlerHub hub;
        private readonly TagChangeTrackerManager trackerManager;
        private readonly ProcessInfoStatistics processInfo;
        private ConcurrentDictionary<WebSocketHandler, List<TagChangeTracker>> wsTags;
        private TimeSpan refreshInterval;

        class SetValueParam
        {
            [JsonPropertyName("topic")]
            public string Topic { get; set; }

            [JsonPropertyName("value")]
            public JsonElement Value { get; set; }
        }

        public RealTimeRpcWebSocketMiddleware(
            IRtConnector connector,
            WebSocketHandlerHub hub,
            TagChangeTrackerManager trackerManager,
            ILoggerFactory loggerFactory,
            ProcessInfoStatistics processInfo,
            TimeSpan? refreshInterval = default)
        {
            this.logger = loggerFactory.CreateLogger(nameof(RealTimeRpcWebSocketMiddleware));
            this.hub = hub;
            this.trackerManager = trackerManager;
            this.processInfo = processInfo;
            this.connector = connector;
            this.wsTags = new ConcurrentDictionary<WebSocketHandler, List<TagChangeTracker>>();
            this.refreshInterval = refreshInterval ?? TimeSpan.FromSeconds(1);

            this.connector.Connected += (s, e) => { processInfo.ConnectedDateTime = DateTime.Now; };
        }

        public override IMessage ProcessMessage(WebSocketHandler ws, IMessage msg)
        {
            // RealTime Subscriber / Unsubscriber
            switch (msg.MethodName)
            {
                case "rpc.rt.subscribe":
                    object result = RtSubscribe(ws, msg.GetParameters<string[]>());
                    return Rpc.JsonRpc.Message.CreateResponse(msg, result);

                case "rpc.rt.unsubscribe":
                    result = RtUnsubscribe(ws);
                    return Rpc.JsonRpc.Message.CreateResponse(msg, result);

                case "rpc.rt.set_value":
                    result = RtSetValue(msg.GetParameters<SetValueParam[]>());
                    return Rpc.JsonRpc.Message.CreateResponse(msg, result);

                case "rpc.rt.get_topics":
                    result = RtGetTopics();
                    return Rpc.JsonRpc.Message.CreateResponse(msg, result);
            }
            return base.ProcessMessage(ws, msg);
        }

        private object RtSubscribe(WebSocketHandler ws, string[] expressions)
        {
            ws.OnClose -= Ws_OnClose;
            ws.OnClose += Ws_OnClose;
            Dictionary<string, string> result = new Dictionary<string, string>();

            logger.LogInformation("RtSubscribe {ws} expressions:{@expressions}", ws.GetHashCode(), expressions);
            lock (wsTags)
            {
                wsTags[ws] = expressions
                    .Select(expression => trackerManager.GetOrAdd(connector, expression))
                    .ToList();
            }

            return new
            {
                tags = wsTags[ws].Select(t => new
                {
                    tagName = t.TagName,
                    status = t.Status,
                    updated = t.Updated,
                    value = t.ValueObject
                }).ToArray()
            };
        }

        private string RtUnsubscribe(WebSocketHandler ws)
        {
            ws.OnClose -= Ws_OnClose;
            logger.LogInformation("RtUnsubscribe {ws}", ws.GetHashCode());
            lock (wsTags) wsTags.Remove(ws, out _);
            return "ok";
        }

        private IEnumerable<string> RtGetTopics()
        {
            return connector.KnownTopics;
        }


        private bool RtSetValue(SetValueParam[] toSets)
        {
            if (toSets.Any(t => RtTagExpression.IsExpression(t.Topic)))
            {
                return false;
            }

            logger.LogInformation("RtSetValue {@toSets}", System.Text.Json.JsonSerializer.Serialize(toSets));

            return toSets.All(t =>
            {
                if (t.Value.ValueKind == JsonValueKind.Number)
                {
                    return connector.SetNumeric(t.Topic, t.Value.GetDouble());
                }
                else if (t.Value.ValueKind == JsonValueKind.Array || t.Value.ValueKind == JsonValueKind.Object)
                {
                    return connector.SetJson(t.Topic, t.Value);
                }
                else
                {
                    return connector.SetText(t.Topic, t.Value.ToString());
                }
            });
        }

        private void Ws_OnClose(object sender, bool e)
        {
            if (sender is WebSocketHandler ws)
            {
                RtUnsubscribe(ws);
            }
        }

        private Task NotifyTagsAsync(WebSocketHandler ws, IEnumerable<TagChangeTracker> tags)
        {
            var msgNotifyTags = new
            {
                notification = "rt.notify.tags",
                method = "rt.notify.tags",
                @params = new
                {
                    tags = tags.Select(t => new
                    {
                        tagName = t.TagName,
                        status = t.Status,
                        updated = t.Updated,
                        value = t.ValueObject
                    }).ToList()
                }
            };
            byte[] msg = JsonSerializer.SerializeToUtf8Bytes(msgNotifyTags);
            return ws.SendAsync(msg);
        }


        private void NotifyTagChange(List<TagChangeTracker> tagsChanged)
        {
            processInfo.TagsCount = trackerManager.GetTagsCount();
            processInfo.TagsChanged += tagsChanged.Count;

            var elements = wsTags
                .Select(wst => new
                {
                    ws = wst.Key,
                    wsTagsChanged = tagsChanged.Where(t => wst.Value.Contains(t)).ToList()
                })
                .Where(e => e.wsTagsChanged.Any());

            foreach (var e in elements)
            {
                NotifyTagsAsync(e.ws, e.wsTagsChanged);
            } 
        }

        public override Task RunAsync(CancellationToken? cancellationToken = null)
        {
            processInfo.LoopPublishStatistics(TimeSpan.FromSeconds(10), cancellationToken);

            return Task.Run(async () =>
            {
                logger.LogInformation("Execution Started: {time}", DateTimeOffset.Now);

                while (true)
                {
                    processInfo.RunningDateTime = DateTime.Now;
                    processInfo.ConnectedClients = wsTags.Keys.Count();
                    var tagsChanged = trackerManager.GetTagsChanged();
                    if (tagsChanged.Any())
                    {
                        logger.LogDebug("tagsChanged: {tagsChanged}", tagsChanged.Count());
                        NotifyTagChange(tagsChanged);
                    }
                    await Task.Delay(refreshInterval);
                }
            }).ContinueWith((t) =>
            {
                logger.LogInformation("Execution Stoped: {time}", DateTimeOffset.Now);
            });
        }
    }
}
