namespace DeltaX.Rpc.JsonRpc.WebSocketConnection
{
    using DeltaX.Connections.WebSocket;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    class EventsRpcWs
    {

        Dictionary<string, HashSet<WebSocketHandler>> eventsWs;

        public EventsRpcWs()
        {
            eventsWs = new Dictionary<string, HashSet<WebSocketHandler>>();
        }

        public Dictionary<string, string> Subscribe(WebSocketHandler ws, string[] events)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (var ev in events)
            {
                if (!eventsWs.ContainsKey(ev))
                {
                    eventsWs[ev] = new HashSet<WebSocketHandler>();
                }

                eventsWs[ev].Add(ws);
                result[ev] = "ok";
            }

            return result;
        }

        public Dictionary<string, string> Unsubscribe(WebSocketHandler ws, string[] events)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (var ev in events)
            {
                eventsWs[ev]?.Remove(ws);
                result[ev] = "ok";
            }

            return result;
        }

        public void Remove(WebSocketHandler ws)
        {
            foreach (var ev in eventsWs.Values.ToArray())
            {
                ev?.Remove(ws);
            }
        }

        public void Notify(IMessage message)
        {
            /// FIXME: Duplico method y notification por compatibilidad con libreria que usa 'notification' :(
            /// https://github.com/elpheria/rpc-websockets/blob/master/src/lib/client.ts
            var msgNoty = new { notification = message.MethodName, method = message.MethodName, @params = message.GetParameters<object>() };
            byte[] msg = JsonSerializer.SerializeToUtf8Bytes(msgNoty);

            foreach (var ev in eventsWs)
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
    }
}
