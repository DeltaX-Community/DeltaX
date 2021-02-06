namespace DeltaX.Rpc.JsonRpc.HttpConnection
{
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class JsonRcpHttpClientConnection : IRpcConnection
    {
        public event EventHandler<IMessage> OnReceive;
        public event EventHandler<bool> OnConnectionChange;
        private HttpClient httpClient;
        private string prefix;
        private IEnumerable<string> registeredMethods;

        public JsonRcpHttpClientConnection(HttpClient httpClient = null, string prefix = null)
        { 

            this.httpClient = httpClient ?? new HttpClient();
            this.prefix = prefix ?? "";
        }

        public string ClientId { get; private set; } = Guid.NewGuid().ToString("N");

        public bool IsConnected()
        {
            return true;
        }

        private string UriRequest(string method)
        {
            return $"{prefix}/request/{method}";
        }

        private string UriNotification(string method)
        {
            return $"{prefix}/notification/{method}";
        }
  
        private async Task GetNotifications(string uri)
        {
            try
            { 
                var response = await httpClient.GetAsync(uri);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var message = Message.Parse(json);

                    OnReceive?.Invoke(this, message);
                }
            }catch(Exception e)
            {
                Console.WriteLine("GetNotifications {0}", e);
            }
        }


        public async Task SendNotificationAsync(IMessage message)
        { 
            var content = new StringContent(message.Serialize(), null, "application/json");
            await httpClient.PostAsync(UriRequest(message.MethodName), content);
        }

        public async Task<IMessage> SendRequestAsync(IMessage message)
        {
            var content = new StringContent(message.Serialize(), null, "application/json");
            var response = await httpClient.PostAsync(UriRequest(message.MethodName), content);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new HttpRequestException("StatusCode " + response.StatusCode);
            }
            var json = await response.Content.ReadAsStringAsync();
            return Message.Parse(json);
        }

        public Task SendResponseAsync(IMessage message)
        {
            throw new NotImplementedException();
        }

        public bool UpdateRegisteredMethods(IEnumerable<string> methods)
        {
            registeredMethods = methods;
            foreach (var method in registeredMethods)
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        GetNotifications(UriNotification(method)).Wait();
                    }
                });
            }

            return true;
        }
    }
}
