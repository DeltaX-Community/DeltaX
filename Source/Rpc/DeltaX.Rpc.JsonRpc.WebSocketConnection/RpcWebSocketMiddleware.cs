namespace DeltaX.Rpc.JsonRpc.WebSocketConnection
{
    using DeltaX.Connections.WebSocket;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class RpcWebSocketMiddleware : IRpcWebSocketMiddleware
    {
        private ConcurrentDictionary<string, HashSet<WebSocketHandler>> subscribedWebsockets;
        public event EventHandler<IEnumerable<string>> SubscritionsChange;

        public RpcWebSocketMiddleware()
        {
            subscribedWebsockets = new ConcurrentDictionary<string, HashSet<WebSocketHandler>>();
        }

        public virtual bool Login(WebSocketHandler ws, IMessage message)
        {
            return true;
        }

        public virtual IMessage ProcessMessage(WebSocketHandler ws, IMessage msg)
        {
            // WebSocket Events Subscriber / Unsubscriber
            if (msg.MethodName == "rpc.on")
            {
                var result = Subscribe(ws, msg.GetParameters<string[]>());
                return JsonRpc.Message.CreateResponse(msg, result);
            }
            else if (msg.MethodName == "rpc.off")
            {
                var result = Unsubscribe(ws, msg.GetParameters<string[]>());
                return JsonRpc.Message.CreateResponse(msg, result);
            }
            else if (msg.MethodName == "rpc.login")
            {
                var result = Login(ws, msg);
                return JsonRpc.Message.CreateResponse(msg, result);
            }
            return null;
        }

        public virtual Dictionary<string, string> Subscribe(WebSocketHandler ws, string[] events)
        {
            ws.OnClose -= Ws_OnClose;
            ws.OnClose += Ws_OnClose;
            Dictionary<string, string> result = new Dictionary<string, string>();

            lock (subscribedWebsockets)
            {
                foreach (var ev in events)
                {
                    if (!subscribedWebsockets.ContainsKey(ev))
                    {
                        subscribedWebsockets[ev] = new HashSet<WebSocketHandler>();
                    }

                    subscribedWebsockets[ev].Add(ws);
                    result[ev] = "ok";
                }
            }

            SubscritionsChange?.Invoke(this, subscribedWebsockets.Keys);

            return result;
        }

        private void Ws_OnClose(object sender, bool e)
        {
            if (sender is WebSocketHandler ws)
            {
                Remove(ws);
            }
        }

        public virtual Dictionary<string, string> Unsubscribe(WebSocketHandler ws, string[] events)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            lock (subscribedWebsockets)
            {
                foreach (var ev in events)
                {
                    subscribedWebsockets[ev]?.Remove(ws);
                    if (!subscribedWebsockets[ev].Any())
                    {
                        subscribedWebsockets.Remove(ev, out _);
                    }
                    result[ev] = "ok";
                }
            }

            SubscritionsChange?.Invoke(this, subscribedWebsockets.Keys);

            return result;
        }

        public virtual void Remove(WebSocketHandler ws)
        {
            ws.OnClose -= Ws_OnClose;

            lock (subscribedWebsockets)
            {
                foreach (var ev in subscribedWebsockets.ToArray())
                {
                    ev.Value?.Remove(ws);
                    if (!ev.Value.Any())
                    {
                        subscribedWebsockets.Remove(ev.Key, out _);
                    }
                }
            }
            SubscritionsChange?.Invoke(this, subscribedWebsockets.Keys);
        }

        public virtual void Notify(IMessage message)
        {
            /// FIXME: Duplico method y notification por compatibilidad con libreria que usa 'notification' :(
            /// https://github.com/elpheria/rpc-websockets/blob/master/src/lib/client.ts
            var msgNoty = new { notification = message.MethodName, method = message.MethodName, @params = message.GetParameters<object>() };
            byte[] msg = JsonSerializer.SerializeToUtf8Bytes(msgNoty);

            foreach (var ev in subscribedWebsockets)
            {
                if (ev.Key == message.MethodName && ev.Value != null)
                {
                    foreach (var ws in ev.Value.ToArray())
                    {
                        ws?.SendAsync(msg);
                    }
                }
            }
        }

        public virtual Task RunAsync(CancellationToken? cancellationToken = null)
        {
            return Task.CompletedTask;
        } 
    }
}
