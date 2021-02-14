namespace DeltaX.Rpc.JsonRpc.WebSocketConnection
{
    using DeltaX.Connections.WebSocket;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    class JsonRpcClientWebSocketConnection : IRpcConnection
    {
        private WebSocketHandler client;
        private TimeSpan timeout;
        private ConcurrentDictionary<object, TaskCompletionSource<IMessage>> requestPending;

        public JsonRpcClientWebSocketConnection(WebSocketHandler client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.client.OnMessageReceive += Client_OnMessageReceive;
        }

        public string ClientId => throw new NotImplementedException();

        public event EventHandler<IMessage> OnReceive;
        public event EventHandler<bool> OnConnectionChange;

        private void Client_OnMessageReceive(object sender, Message e)
        {
            var msg = JsonRpc.Message.Parse(e.Data);

            TaskCompletionSource<IMessage> promise;
            if (msg.IsResponse())
            {
                if (requestPending.TryRemove(msg.Id, out promise))
                {
                    promise.SetResult(msg);
                }
            }
            else
            {
                OnReceive?.Invoke(this, msg);
            }
        }
         

        public bool IsConnected()
        {
            return true;
        }

        public Task SendNotificationAsync(IMessage message)
        {
            return client.SendAsync(message.SerializeToBytes());
        }

        public Task<IMessage> SendRequestAsync(IMessage message)
        {
            var promise = new TaskCompletionSource<IMessage>();
            requestPending.TryAdd(message.Id, promise); 

            client.SendAsync(message.SerializeToBytes()).Wait();

            promise.Task.ContinueWith((t) =>
            {
                requestPending.TryRemove(message.Id, out _);
            });

            return promise.Task;
        }

        public Task SendResponseAsync(IMessage message)
        {
            throw new NotImplementedException();
        }

        public bool UpdateRegisteredMethods(IEnumerable<string> methods)
        {
            return true;
        }

        public Task ConnectAsync(CancellationToken? cancellationToken = null)
        {
            throw new NotImplementedException();
        }
    }
}
