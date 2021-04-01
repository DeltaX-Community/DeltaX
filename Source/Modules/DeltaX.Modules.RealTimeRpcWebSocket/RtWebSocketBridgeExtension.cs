namespace DeltaX.Modules.RealTimeRpcWebSocket
{
    using DeltaX.Connections.WebSocket; 
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using DeltaX.Rpc.JsonRpc.WebSocketConnection;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using DeltaX.RealTime.Interfaces;
    using Microsoft.Extensions.Options;
    using DeltaX.Modules.RealTimeRpcWebSocket.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.AspNetCore.Builder;

    public static class RtWebSocketBridgeExtension
    {

        public static IHostBuilder UseRealTimeWebSocketServices(
            this IHostBuilder builder)
        {
            return builder.ConfigureServices((hostContext, services) =>
            {
                services.Configure<RtWebSocketBridgeConfiguration>(options =>
                    hostContext.Configuration.GetSection("RtWebSocketBridge").Bind(options));

                services.AddRealTimeWebSocketServices();
            });
        }

        public static IServiceCollection AddRealTimeWebSocketServices(
            this IServiceCollection services)
        {
            services.AddSingleton<ProcessInfoStatistics>();
            services.AddSingleton<WebSocketHandlerHub>();
            services.AddSingleton<TagChangeTrackerManager>();
            services.AddSingleton<IRpcWebSocketMiddleware>(serv =>
            {
                var logFactory = serv.GetService<ILoggerFactory>();
                var processInfo = serv.GetService<ProcessInfoStatistics>();
                var trackerManager = serv.GetService<TagChangeTrackerManager>();
                var hub = serv.GetService<WebSocketHandlerHub>();
                var conn = serv.GetService<IRtConnector>();
                var config = serv.GetService<IOptions<RtWebSocketBridgeConfiguration>>();
                var interval = config?.Value.RtWebSocketRefreshIntervalMs ?? 200;
                var refreshInterval = TimeSpan.FromMilliseconds(interval);

                return new RealTimeRpcWebSocketMiddleware(conn, hub, trackerManager, logFactory, processInfo, refreshInterval);
            });
            services.AddSingleton<IRpcConnection, JsonRpcWebSocketConnection>();

            return services;
        }

        public static IApplicationBuilder UseRealTimeWebSocketBridge(this IApplicationBuilder app, string mapPrefix = "/rt")
        {
            var hub = app.ApplicationServices.GetService<WebSocketHandlerHub>();  
            // var webSocketOptions = new WebSocketOptions()
            // {
            //     KeepAliveInterval = TimeSpan.FromSeconds(120),
            // };
            // webSocketOptions.AllowedOrigins.Add("https://client.com");
            // webSocketOptions.AllowedOrigins.Add("https://www.client.com");
            // 
            // app.UseWebSockets(webSocketOptions);

            return app 
                .Map(mapPrefix, app =>
                { 
                    app.Use(async (context, next) =>
                    { 
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                            await hub.RegisterWebSocket(webSocket).ReceiveAsync();
                            return;
                        }
                        await next();
                    });
                });
        }
    }

}
