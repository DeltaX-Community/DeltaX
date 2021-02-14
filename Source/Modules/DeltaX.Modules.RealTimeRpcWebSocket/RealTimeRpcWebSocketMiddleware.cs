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
            ILoggerFactory loggerFactory,
            TimeSpan? refreshInterval = default)
        {
            this.logger = loggerFactory.CreateLogger(nameof(RealTimeRpcWebSocketMiddleware));
            this.hub = hub;
            this.connector = connector;
            this.wsTags = new ConcurrentDictionary<WebSocketHandler, List<TagChangeTracker>>();
            this.refreshInterval = refreshInterval ?? TimeSpan.FromSeconds(1);
        }

        public override IMessage ProcessMessage(WebSocketHandler ws, IMessage msg)
        {
            // RealTime Subscriber / Unsubscriber
            if (msg.MethodName == "rpc.rt.subscribe")
            {
                var result = RtSubscribe(ws, msg.GetParameters<string[]>());
                return Rpc.JsonRpc.Message.CreateResponse(msg, result);
            }
            else if (msg.MethodName == "rpc.rt.unsubscribe")
            {
                var result = RtUnsubscribe(ws);
                return Rpc.JsonRpc.Message.CreateResponse(msg, result);
            }
            else if (msg.MethodName == "rpc.rt.set_value")
            {
                var result = RtSetValue(msg.GetParameters<SetValueParam[]>());
                return Rpc.JsonRpc.Message.CreateResponse(msg, result);
            }
            else if (msg.MethodName == "rpc.rt.get_topics")
            {
                var result = RtGetTopics();
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
                    .Select(expression => TagChangeTracker.GetOrAdd(connector, expression))
                    .ToList();
            }

            return new
            {
                tags = wsTags[ws].Select(t => new { t.TagName, t.Status, t.Updated, Value = t.ValueObject }).ToArray()
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

            logger.LogInformation("RtSetValue {@toSets}", toSets);

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

        private void NotifyTags(WebSocketHandler ws, IEnumerable<TagChangeTracker> tags)
        {
            var msgNotifyTags = new
            {
                notification = "rt.notify.tags",
                method = "rt.notify.tags",
                @params = new
                {
                    tags = tags.Select(t => new { t.TagName, t.Status, t.Updated, Value = t.ValueObject }).ToArray()
                }
            };
            byte[] msg = JsonSerializer.SerializeToUtf8Bytes(msgNotifyTags);
            ws.SendAsync(msg);
        }


        private void NotifyTagChange(TagChangeTracker[] tagsChanged)
        {
            foreach (var wst in wsTags)
            {
                var tags = tagsChanged.Where(t => wst.Value.Contains(t)).ToArray();
                if (tags.Any())
                {
                    NotifyTags(wst.Key, tags);
                }
            }
        }

        public override Task RunAsync(CancellationToken? cancellationToken = null)
        {
            var tLogger = Task.Run(async () =>
            {
                while (true)
                {
                    logger.LogInformation("running at {time}. Clients connected:{clients}", DateTimeOffset.Now, hub.GetClients().Count());
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
            });


            var tWorker = Task.Run(async () =>
            {
                logger.LogWarning("Execution Started: {time}", DateTimeOffset.Now);

                while (true)
                {
                    var tagsChanged = TagChangeTracker.GetTagsChanged();
                    if (tagsChanged.Any())
                    {
                        logger.LogDebug("tagsChanged: {tagsChanged}", tagsChanged.Count());
                        NotifyTagChange(tagsChanged);
                    }
                    await Task.Delay(refreshInterval);
                }
            });

            return Task.WhenAny(tLogger, tWorker)
                .ContinueWith((t) =>
                {
                    logger.LogWarning("Execution Stoped: {time}", DateTimeOffset.Now);
                });
        }
    }
}
