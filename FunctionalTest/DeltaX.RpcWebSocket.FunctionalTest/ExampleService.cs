

namespace DeltaX.RpcWebSocket.FunctionalTest
{
    using DeltaX.Connections.WebSocket;
    using DeltaX.RealTime.Interfaces;
    using DeltaX.RealTime.RtExpression;
    using DeltaX.RealTime;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using DeltaX.Rpc.JsonRpc.WebSocketConnection;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    class TagChangeTracker
    {
        static List<TagChangeTracker> cacheTags = new List<TagChangeTracker>();

        public static TagChangeTracker GetOrAdd(IRtConnector connector, string tagExpression)
        {
            lock (cacheTags)
            {
                var t = cacheTags.FirstOrDefault(t => t.TagName == tagExpression);
                if (t == null)
                {
                    var tag = (RtTagExpression) RtTagExpression.AddExpression(connector, tagExpression);
                    t = new TagChangeTracker { tag = tag, TagName = tagExpression };
                    cacheTags.Add(t);
                }
                return t;
            }
        }

        public static TagChangeTracker[] GetTagsChanged()
        {
            return cacheTags.Where(t => t.IsChanged()).ToArray();
        }

        private RtTagExpression tag;
        public string TagName { get; private set; }
        public DateTime Updated { get; set; }
        public DateTime PrevUpdated { get; set; }
        public IRtValue Value { get; set; }
        public IRtValue PrevValue { get; set; }
        public bool Status { get; set; }
        public bool PrevStatus { get; set; }
        public object ValueObject => double.IsNaN(Value.Numeric) || double.IsInfinity(Value.Numeric)
            ? Value.Text
            : Value.Numeric;

        public bool IsChanged()
        {
            PrevStatus = Status;
            PrevUpdated = Updated;
            PrevValue = Value;
            Status = tag.Status;
            Updated = tag.Updated;
            Value = tag.Value;

            return PrevStatus != Status || PrevUpdated != Updated || PrevValue?.Text != Value.Text;
        }
    }

    class SetValueParam
    {
        [JsonPropertyName("topic")]
        public string Topic { get; set; }

        [JsonPropertyName("value")]
        public JsonElement Value { get; set; }
    }

    public class RealTimeRpcWebSocketMiddleware : RpcWebSocketMiddleware, IRpcWebSocketMiddleware
    {
        private ILogger logger;
        private IRtConnector connector;
        private ConcurrentDictionary<WebSocketHandler, List<TagChangeTracker>> wsTags;

        public RealTimeRpcWebSocketMiddleware (IRtConnector connector, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger(nameof(RealTimeRpcWebSocketMiddleware));
            this.connector = connector;
            this.wsTags = new ConcurrentDictionary<WebSocketHandler, List<TagChangeTracker>>(); 
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
            return base.ProcessMessage(ws, msg);
        }

        private string RtSubscribe(WebSocketHandler ws, string[] expressions)
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

            return "ok";
        }

        private string RtUnsubscribe(WebSocketHandler ws)
        {
            ws.OnClose -= Ws_OnClose;
            logger.LogInformation("RtUnsubscribe {ws}", ws.GetHashCode());
            lock (wsTags) wsTags.Remove(ws, out _);
            return "ok";
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

        private void NotifyTagChange(TagChangeTracker[] tagsChanged)
        {
            foreach (var wst in wsTags)
            {
                var tags = tagsChanged.Where(t => wst.Value.Contains(t)).ToArray();
                if (tags.Any())
                {
                    var msgNotifyTags = new {
                        notification = "rt.notify.tags",
                        method = "rt.notify.tags",
                        @params = new
                        {
                            tags = tags.Select(t => new { t.TagName, t.Status, t.Updated, Value = t.ValueObject }).ToArray()
                        }
                    };
                    byte[] msg = JsonSerializer.SerializeToUtf8Bytes(msgNotifyTags);

                    var str = System.Text.ASCIIEncoding.ASCII.GetString(msg); 
                    logger.LogInformation("NotifyTagChange ws:{ws} Message:{@msg}", wst.Key, str);

                    wst.Key.SendAsync(msg);
                }
            }
        }

        public void ForceRefreshTags()
        {
            var tagsChanged = TagChangeTracker.GetTagsChanged();
            if (tagsChanged.Any())
            {
                logger.LogInformation("ForceRefreshTags {@tagsChanged}", tagsChanged);
                NotifyTagChange(tagsChanged);
            }
        }
    }

    public class ExampleService : IExampleService
    {
        private readonly Rpc.JsonRpc.Rpc rpc;
        private readonly ILogger logger;

        public ExampleService(Rpc.JsonRpc.Rpc rpc)
        {
            this.rpc = rpc;
        }

        public int Sum(int a, int b)
        {
            var result = a + b;

            this.rpc.Notify("feedUpdated", result);
            return result;
        }
    }
}
