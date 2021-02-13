

namespace DeltaX.RpcWebSocket.FunctionalTest
{
    using DeltaX.CommonExtensions;
    using DeltaX.Connections.WebSocket;
    using DeltaX.Rpc.JsonRpc.WebSocketConnection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class WebSocketsWorker : BackgroundService
    {
        private readonly ILogger logger;
        private readonly WebSocketHandlerHub hub;
        private readonly RealTimeRpcWebSocketMiddleware rtWs;
        private readonly Rpc.JsonRpc.Rpc rpc;

        public WebSocketsWorker(ILogger<WebSocketsWorker> logger, WebSocketHandlerHub hub, RealTimeRpcWebSocketMiddleware rtWs, Rpc.JsonRpc.Rpc rpc)
        {
            this.logger = logger;
            this.hub = hub;
            this.rtWs = rtWs;
            this.rpc = rpc;
            IExampleService service = new ExampleService(rpc);
            this.rpc.Dispatcher.RegisterService(service);
            this.rpc.UpdateRegisteredMethods(); 
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            { 

                logger.LogWarning("Execution Started: {time}", DateTimeOffset.Now);
                 
                while (!stoppingToken.IsCancellationRequested )
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    logger.LogInformation("clients {clients}", hub.GetClients().Count());


                    rtWs.ForceRefreshTags();
                    await Task.Delay(1000, stoppingToken);
                }
            }).ContinueWith((t) =>
            {
                logger.LogWarning("Execution Stoped: {time}", DateTimeOffset.Now); 
            }); 
        }
    }
}
