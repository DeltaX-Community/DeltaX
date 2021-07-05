namespace DeltaX.Rpc.JsonRpc.HttpConnection
{
    using DeltaX.Connections.HttpServer;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class JsonRcpHttpConnection : IRpcConnection
    {
        public event EventHandler<IMessage> OnReceive;
        public event EventHandler<bool> OnConnectionChange;

        private bool connected;
        private Listener listener;
        private ConcurrentDictionary<object, Response> tasksToReply = new ConcurrentDictionary<object, Response>();
        private IEnumerable<string> registeredMethods;
        private string prefix;
        private List<(string endpoint, Response response)> subscriptions = new List<(string, Response)>(100);

        public string ClientId { get; private set; } = Guid.NewGuid().ToString("N");

        public JsonRcpHttpConnection(Listener listener, string prefix = null)
        {
            this.listener = listener ?? new Listener(); 
            this.listener.OnRequest += OnRequest;
            this.prefix = prefix ?? "";
        }

        private async Task OnRequest(Request request, Response response)
        {
            if (request.Method == HttpMethod.Get
                && request.Endpoint.StartsWith($"{prefix}/notification/"))
            {
                if (subscriptions.Count == subscriptions.Capacity)
                {
                    var recipient = subscriptions[0];
                    subscriptions.RemoveAt(0);

                    _ = recipient.response.CloseAsync(HttpStatusCode.RequestTimeout);
                }

                subscriptions.Add((request.Endpoint, response));
                return;
            }

            if (request.Method == HttpMethod.Post
                && registeredMethods != null
                && registeredMethods.Contains(request.Endpoint))
            {
                var json = await request.GetBodyAsync();

                var message = Message.Parse(json);
                if (message.IsRequest())
                {
                    tasksToReply[message.Id] = response;
                    OnReceive?.Invoke(this, message);
                    return;
                }
                OnReceive?.Invoke(this, message);
            }

            await response.CloseAsync(HttpStatusCode.NotFound);
            return;
        }

        public bool IsConnected()
        {
            return connected;
        }

        public Task SendNotificationAsync(IMessage message)
        {
            string json = null;
            var endpoint = UriNotification(message.MethodName);
            var recipients = subscriptions.Where(s => s.endpoint == endpoint).ToArray();

            foreach (var recipient in recipients)
            {
                json ??= message.Serialize();
                _ = recipient.response.SendAsync(json, "application/json");
                subscriptions.Remove(recipient);
            }

            return Task.CompletedTask;
        }

        public Task<IMessage> SendRequestAsync(IMessage message)
        {
            throw new NotImplementedException();
        }

        public async Task SendResponseAsync(IMessage message)
        {
            Response response;
            if (tasksToReply.TryRemove(message.Id, out response))
            {
                var json = message.Serialize();
                await response.SendAsync(json, "application/json");
            }
            return;
        }

        private string UriNotification(string method)
        {
            return $"{prefix}/notification/{method}";
        }


        private string UriRequest(string method)
        {
            return $"{prefix}/request/{method}";
        }

        public bool UpdateRegisteredMethods(IEnumerable<string> methods)
        {
            registeredMethods = methods.Select(m => UriRequest(m));
            return true;
        }

        public Task RunAsync(CancellationToken? cancellationToken = null)
        {
            this.connected = true;
            return listener.StartAsync()
                .ContinueWith((t) =>
                {
                    this.connected = false;
                });
        }
    }
}
