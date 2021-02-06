namespace DeltaX.Rpc.JsonRpc.WebSocketConnection
{
    using DeltaX.Connections.WebSocket;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;


    public class JsonRpcWebSocketConnection : IRpcConnection
    {
        private string prefix;
        private ConcurrentDictionary<string, TaskCompletionSource<IMessage>> requestPending;
        private ConcurrentDictionary<string, WebSocketHandler> tasksToReply = new ConcurrentDictionary<string, WebSocketHandler>();
        private IEnumerable<string> registeredMethods;
        private WebSocketServer server;
        private DateTimeOffset uptime;
        private EventsRpcWs eventsRpcWs;

        public JsonRpcWebSocketConnection(WebSocketServer server, string prefix = null, string clientId = null )
        {
            this.server = this.server ?? throw new ArgumentNullException(nameof(server));
            this.prefix = prefix ?? "";
            this.requestPending = new ConcurrentDictionary<string, TaskCompletionSource<IMessage>>();
            this.registeredMethods = new string[0];
            this.ClientId = clientId ?? Guid.NewGuid().ToString("N");
            this.eventsRpcWs = eventsRpcWs = new EventsRpcWs(); 
            this.uptime = new DateTimeOffset(DateTime.Now); 

            this.server.Hub.OnMessageReceive += Hub_OnMessageReceive;
        }


        public event EventHandler<IMessage> OnReceive;

        public event EventHandler<bool> OnConnectionChange;

        public string ClientId { get; private set; }


        private void Hub_OnMessageReceive(object sender, Message e) 
        {
            var msg = JsonRpc.Message.Parse(e.Data);
                        
            if (msg.IsRequest())
            {
                // WebSocket Events Subscriber / Unsubscriber
                if (msg.MethodName == "rpc.on")
                {
                    var result = eventsRpcWs.Subscribe(e.Client, msg.GetParameters<string[]>());
                    SendResponseAsync(JsonRpc.Message.CreateResponse(msg, result));
                    return;
                }
                else if (msg.MethodName == "rpc.off")
                {
                    var result = eventsRpcWs.Unsubscribe(e.Client, msg.GetParameters<string[]>());
                    SendResponseAsync(JsonRpc.Message.CreateResponse(msg, result));
                    return;
                }
                else
                {
                    tasksToReply[msg.Id] = e.Client; 
                }
            } 
              
            OnReceive?.Invoke(this, msg);             
        }

        public Task SendNotificationAsync(IMessage message)
        {
            eventsRpcWs.Notify(message);
            return Task.CompletedTask;
        }

        public Task SendResponseAsync(IMessage message)
        {
            WebSocketHandler client;
            if (tasksToReply.TryRemove(message.Id, out client))
            {
                return client.SendAsync(message.SerializeToBytes());
            }
            return Task.CompletedTask;
        }

        public bool UpdateRegisteredMethods(IEnumerable<string> methods)
        {
            registeredMethods = methods.ToArray();
            return true;
        }

        public bool IsConnected()
        {
            return true;
        }

        public Task<IMessage> SendRequestAsync(IMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
