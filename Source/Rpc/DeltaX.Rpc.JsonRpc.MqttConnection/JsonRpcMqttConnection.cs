namespace DeltaX.Rpc.JsonRpc.MqttConnection
{
    using DeltaX.Connections.MqttClientHelper;
    using DeltaX.Rpc.JsonRpc;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic; 
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using uPLibrary.Networking.M2Mqtt;
    using uPLibrary.Networking.M2Mqtt.Messages;

    public class JsonRpcMqttConnection : IRpcConnection
    {
        private string prefix;
        private ConcurrentDictionary<string, TaskCompletionSource<IMessage>> requestPending;
        private IEnumerable<string> registeredMethods;
        private MqttClientHelper client;
        private DateTimeOffset uptime;

        public JsonRpcMqttConnection(MqttClientHelper client, string prefix = null, string clientId = null)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));           
            this.prefix = prefix ?? "";
            this.requestPending = new ConcurrentDictionary<string, TaskCompletionSource<IMessage>>();
            this.registeredMethods = new string[0];
            this.ClientId = clientId ?? Guid.NewGuid().ToString("N");
            this.uptime = new DateTimeOffset(DateTime.Now);

            this.client.Client.ConnectionClosed += MqttOnConnectionClosed;
            this.client.Client.MqttMsgPublishReceived += MqttOnMessageReceive;

            PublishInfo();
        }

        public event EventHandler<IMessage> OnReceive;

        public event EventHandler<bool> OnConnectionChange;

        public string ClientId { get; private set; } 

        private void MqttOnConnectionClosed(object sender, EventArgs e)
        { 
            OnConnectionChange?.Invoke(this, IsConnected());
        }

        private void MqttOnMessageReceive(object sender, MqttMsgPublishEventArgs e)
        {
            var msg = Message.Parse(e.Message);

            TaskCompletionSource<IMessage> promise;
            if (msg.IsResponse())
            {
                if (requestPending.TryRemove(msg.Id.ToString(), out promise))
                {
                    promise.SetResult(msg);
                }
            }
            else
            {
                OnReceive?.Invoke(this, msg);
            }
        }

        private string GetSubjectServiceInformation()
        {
            return $"{prefix}info/{ClientId}";
        }

        private string GetSubjectResponse(string method, string clientId = null)
        {
            return $"{prefix}{method}/response/{clientId ?? ClientId}";
        }

        private string GetSubjectRequest(string method)
        { 
            return $"{prefix}{method}/request";
        }
         
        public Task SendNotificationAsync(IMessage message)
        {
            this.client.Client.Publish(GetSubjectRequest(message.MethodName), message.SerializeToBytes());
            return Task.CompletedTask;
        }

        public Task<IMessage> SendRequestAsync(IMessage message)
        {
            var promise = new TaskCompletionSource<IMessage>();
            requestPending.TryAdd(message.Id.ToString(), promise);

            this.client.Client.Subscribe(new[] { GetSubjectResponse(message.MethodName) }, new[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            this.client.Client.Publish(GetSubjectRequest(message.MethodName), message.SerializeToBytes());

            promise.Task.ContinueWith((t) =>
            {
                requestPending.TryRemove(message.Id.ToString(), out _);
            });

            return promise.Task;
        }
        
        public Task SendResponseAsync(IMessage message)
        {
            var request = message.GetRequestMessage();
            var clientId = request.Id.ToString().Split(':')[0];
            this.client.Client.Publish(GetSubjectResponse(request.MethodName, clientId), message.SerializeToBytes());
            return Task.CompletedTask;
        }

        public void PublishInfo()
        {
            if (IsConnected())
            {
                var info = new
                {
                    prefix = prefix,
                    ClientId = ClientId,
                    uptime = uptime,
                    methods = registeredMethods.Select(m => GetSubjectRequest(m)).ToArray()
                };

                this.client.Client.Publish(
                    GetSubjectServiceInformation(),
                    JsonSerializer.SerializeToUtf8Bytes(info),
                    MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE,
                    true);
            }
        }

        public void SubscribeAll()
        {
            if (IsConnected() && registeredMethods.Any())
            {
                this.client.Client.Subscribe(
                    registeredMethods.Select(m => GetSubjectRequest(m)).ToArray(),
                    registeredMethods.Select(t => MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE).ToArray()); 
            }

            PublishInfo();
        }

        public bool UpdateRegisteredMethods(IEnumerable<string> methods)
        {
            registeredMethods = methods.ToArray();
            SubscribeAll();
            return true;
        }

        public bool IsConnected()
        {
            return client?.IsConnected == true;
        }

        public Task ConnectAsync(CancellationToken? cancellationToken = null)
        {
            return client.RunAsync(cancellationToken);
        }
    }
}
