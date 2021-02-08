using System;


namespace DeltaX.Connections.WebSocket
{
    using DeltaX.Configuration;
    using DeltaX.Configuration.Serilog;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class WebSocketClient
    {
        ClientWebSocket client;
        ILogger logger;
        Task reconnectTask;
        ManualResetEvent isConnectedEvent;
        ManualResetEvent isDisconnectedEvent;

        public WebSocketClient(Uri uri, ClientWebSocket client = null, ILogger logger = null)
        {
            this.Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            this.client = client ?? new ClientWebSocket();
            this.logger = logger ?? Configuration.DefaultLogger;

            this.IsRunning = false;
            this.isConnectedEvent = new ManualResetEvent(false);
            this.isDisconnectedEvent = new ManualResetEvent(true);
            this.WebSocket = new WebSocketHandler(this.client);
            this.WebSocket.OnClose += WebSocket_OnClose;
        }

        public event EventHandler<bool> OnConnectionChange;

        private void WebSocket_OnClose(object sender, bool e)
        {
            isConnectedEvent.Reset();
            isDisconnectedEvent.Set();
            logger.LogInformation($"WebSocket_OnClose uri:{Uri} Connect Closed!");
            OnConnectionChange?.Invoke(this, IsConnected);
        }

        public bool IsRunning { get; private set; }

        public TimeSpan ReconnectDelay { get; set; }

        public bool IsConnected
        {
            get
            {
                return isConnectedEvent.WaitOne(0)
                    && client.State == WebSocketState.Open;
            }
        }

        public WebSocketHandler WebSocket { get; private set; }

        public Uri Uri { get; set; }

        private async Task DoConnect(CancellationToken cancellationToken)
        {
            Type prevException = null;
            IsRunning = true;
            isConnectedEvent.Reset();
            isDisconnectedEvent.Set();

            while (!cancellationToken.IsCancellationRequested && IsRunning)
            {
                try
                {
                    logger.LogDebug($"WebSocketClient Connecting with uri:{Uri}");

                    await client.ConnectAsync(Uri, cancellationToken);

                    logger.LogInformation($"WebSocketClient Connected uri:{Uri}");

                    prevException = null;
                    isConnectedEvent.Set();
                    isDisconnectedEvent.Reset();

                    OnConnectionChange?.Invoke(this, IsConnected);

                    // block while is connected
                    isDisconnectedEvent.WaitOne();
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
                        logger.LogError(e, "WebSocketClient Connect Error");
                        logger.LogInformation($"WebSocketClient uri:{Uri} Connect Failed, retry on {ReconnectDelay}.");
                    }

                    await Task.Delay(ReconnectDelay, cancellationToken);
                }
                finally
                {
                    isConnectedEvent.Reset();
                    isDisconnectedEvent.Set();
                }
            }

            logger.LogInformation($"WebSocketClient uri:{Uri} Connection Closed!");
            OnConnectionChange?.Invoke(this, IsConnected);
        }

        public Task<bool> ConnectAsync(CancellationToken? cancellationToken = null)
        {
            RunAsync(cancellationToken);
            isConnectedEvent.WaitOne();
            return Task.FromResult(IsConnected);
        }

        public Task RunAsync(CancellationToken? cancellationToken = null)
        {
            if (reconnectTask != null && IsRunning)
            {
                return reconnectTask;
            }

            if (WebSocket == null)
            {
                throw new InvalidOperationException("The object is Disposed!");
            }

            IsRunning = true;
            reconnectTask = Task.Run(() => DoConnect(cancellationToken ?? CancellationToken.None));
            return reconnectTask;
        }

        public void Disconnect()
        {
            WebSocket.OnClose -= WebSocket_OnClose;
            IsRunning = false;
            isConnectedEvent.Reset();
            isDisconnectedEvent.Set();

            WebSocket?.Close();
            WebSocket = null;
            client = null;

            reconnectTask?.Wait(5000);
            reconnectTask = null;
        }
    }
}
