namespace DeltaX.Connections.MqttClientHelper
{
    using DeltaX.Configuration; 
    using Microsoft.Extensions.Logging; 
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using uPLibrary.Networking.M2Mqtt;

    public class MqttClientHelper
    {
        ILogger logger;
        Task reconnectTask;

        ManualResetEventSlim isConnectedEvent;
        ManualResetEventSlim isDisconnectedEvent;

        public event EventHandler<bool> OnConnectionChange;

        public MqttClient Client { get; private set; }

        public bool IsConnected
        {
            get
            {
                return isConnectedEvent.Wait(0)
                    && Client?.IsConnected == true;
            }
        }

        public MqttConfiguration Config { get; set; }

        public bool IsRunning { get; set; }

        public MqttClientHelper(MqttConfiguration config, MqttClient client = null, ILoggerFactory loggerFactory = null)
        {
            loggerFactory ??= Configuration.DefaultLoggerFactory;
            this.logger = loggerFactory.CreateLogger($"{nameof(MqttClientHelper)}");
            this.Config = config;
            isConnectedEvent = new ManualResetEventSlim(false);
            isDisconnectedEvent = new ManualResetEventSlim(true);
            this.Client = client ?? new MqttClient(Config.Host, Config.Port, Config.Secure, null, null, MqttSslProtocols.None);            
        }

        private async Task DoConnect(CancellationToken cancellationToken)
        {
            Type prevException = null;
            isConnectedEvent.Reset();
            isDisconnectedEvent.Set();

            Client.ConnectionClosed -= OnConnectionClosed;
            Client.ConnectionClosed += OnConnectionClosed;

            while (!cancellationToken.IsCancellationRequested && IsRunning)
            {
                try
                {
                    // logger.LogDebug($"MqttClientHelper Connecting with ClientId:{Config.ClientId}");
                    if (string.IsNullOrEmpty(Config.Username))
                    {
                        Client.Connect(Config.ClientId);
                    }
                    else
                    {
                        Client.Connect(Config.ClientId, Config.Username, Config.Password);
                    } 

                    logger.LogInformation($"MqttClientHelper Connected ClientId:{Config.ClientId}");

                    prevException = null; 
                    isConnectedEvent.Set();
                    isDisconnectedEvent.Reset();
                    
                    OnConnectionChange?.Invoke(this, IsConnected);

                    // block while is connected
                    isDisconnectedEvent.Wait(cancellationToken);
                }
                catch (Exception e)
                {
                    if (cancellationToken.IsCancellationRequested || !IsRunning)
                    {
                        break;
                    }

                    if (e.GetType() != prevException)
                    {
                        prevException = e.GetType();
                        logger.LogError(e, "MqttClientHelper Connect Error");
                        logger.LogWarning($"MqttClientHelper ClientId:{Config.ClientId} Connect Failed, retry on {Config.ReconnectDealy / 1000} seconds");
                    }

                    await Task.Delay(Config.ReconnectDealy, cancellationToken);
                }
                finally
                {
                    isConnectedEvent.Reset();
                    isDisconnectedEvent.Set();
                }
            }

            if (Client != null)
            {
                Client.ConnectionClosed -= OnConnectionClosed;
                Client.Disconnect();
            }
            logger.LogInformation($"MqttClientHelper ClientId:{Config.ClientId} Connection Closed!");
            OnConnectionChange?.Invoke(this, IsConnected);
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            isDisconnectedEvent.Set();
            logger.LogInformation($"MqttClientHelper ClientId:{Config.ClientId} Connect Closed!");
            OnConnectionChange?.Invoke(this, IsConnected);
        }


        public Task<bool> ConnectAsync(CancellationToken? cancellationToken = null)
        {
            return Task.Run(() =>
            {
                RunAsync(cancellationToken);
                isConnectedEvent.Wait(cancellationToken ?? CancellationToken.None);
                return Task.FromResult(IsConnected);
            });
        }

        public Task RunAsync(CancellationToken? cancellationToken = null)
        {
            lock (this)
            {
                if (reconnectTask != null && IsRunning)
                {
                    return reconnectTask;
                }

                IsRunning = true;
                reconnectTask = Task.Run(() => DoConnect(cancellationToken ?? CancellationToken.None));
                return reconnectTask;
            }
        }

        public void Disconnect()
        {
            try
            {
                IsRunning = false;
                Client.ConnectionClosed -= OnConnectionClosed;
                Client?.Disconnect();
            }
            catch { }
            finally
            {
                isConnectedEvent.Reset();
                isDisconnectedEvent.Set();
                reconnectTask?.Wait(5000);
                reconnectTask = null;
                Client = null;
            }
        }
    }


}
