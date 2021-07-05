

namespace DeltaX.RpcWebSocket.FunctionalTest
{
    using DeltaX.RealTime.Interfaces;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class RealTimeWebSocketBridgeWorker : BackgroundService
    {
        private readonly ILogger logger;
        private readonly IRpcConnection rpcConnection;
        private readonly IRtConnector connector;

        public RealTimeWebSocketBridgeWorker(
            ILogger<RealTimeWebSocketBridgeWorker> logger,
            IRpcConnection rpcConnection,
            IRtConnector connector)
        {
            this.logger = logger;
            this.rpcConnection = rpcConnection;
            this.connector = connector;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {  
            return rpcConnection
                .RunAsync(stoppingToken)
                .ContinueWith((t) =>
                {
                    if (t.IsFaulted)
                    {
                        logger.LogError("Execution RealTimeWebSocketBridgeWorker Stoped: {time} {error}", DateTimeOffset.Now, t.Exception);
                        Environment.Exit(-1);
                    }
                    else
                    {
                        logger.LogWarning("Execution RealTimeWebSocketBridgeWorker Stoped: {time}", DateTimeOffset.Now);
                    }
                });
        }
    }
}
