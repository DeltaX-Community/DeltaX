namespace DeltaX.Rpc.JsonRpc.WebSocketConnection
{
    using DeltaX.Connections.WebSocket;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;


    public class JsonRpcWebSocketConnection : IRpcConnection
    {
        private string prefix;
        private ConcurrentDictionary<string, TaskCompletionSource<IMessage>> requestPending;
        private ConcurrentDictionary<object, WebSocketHandler> tasksToReply = new ConcurrentDictionary<object, WebSocketHandler>();
        private WebSocketHandlerHub serverHub; 
        private IRpcWebSocketMiddleware wsMiddleware;
        private string[] registeredMethods;

        public JsonRpcWebSocketConnection(WebSocketHandlerHub serverHub, IRpcWebSocketMiddleware wsMiddleware,  string clientId = null )
        {
            this.serverHub = serverHub ?? throw new ArgumentNullException(nameof(serverHub)); 
            this.requestPending = new ConcurrentDictionary<string, TaskCompletionSource<IMessage>>(); 
            this.ClientId = clientId ?? Guid.NewGuid().ToString("N");
            this.wsMiddleware = wsMiddleware ?? new RpcWebSocketMiddleware();
            this.registeredMethods = new string[0];

            this.serverHub.OnMessageReceive += Hub_OnMessageReceive; 
        }


        public event EventHandler<IMessage> OnReceive;

        public event EventHandler<bool> OnConnectionChange;

        public string ClientId { get; private set; }


        private void Hub_OnMessageReceive(object sender, Message e) 
        { 
            var msg = JsonRpc.Message.Parse(e.Data);
                        
            if (msg.IsRequest())
            {
                var middlewareResponse = wsMiddleware.ProcessMessage(e.Client, msg); 
                if (middlewareResponse!=null)
                {
                    tasksToReply[msg.Id] = e.Client;
                    SendResponseAsync(middlewareResponse);
                    return;
                } 
                else if (registeredMethods.Contains(msg.MethodName))
                {
                    tasksToReply[msg.Id] = e.Client;
                    e.Client.OnClose -= Client_OnClose;
                    e.Client.OnClose += Client_OnClose;
                }
            }
            
            OnReceive?.Invoke(this, msg);             
        }

        private void Client_OnClose(object sender, bool e)
        {
            if (sender is WebSocketHandler ws)
            {
                lock (tasksToReply)
                {
                    foreach (var p in tasksToReply.Where(pair => pair.Value == ws).ToArray())
                    {
                        tasksToReply.Remove(p.Key, out _);
                    }
                }
            }
        }

        public Task SendNotificationAsync(IMessage message)
        {
            wsMiddleware.Notify(message);
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

        public Task ConnectAsync(CancellationToken? cancellationToken = null)
        {
            return wsMiddleware.RunAsync(cancellationToken);
        }
    }
}
