﻿namespace DeltaX.Modules.RealTimeRpcWebSocket
{
    using DeltaX.Connections.WebSocket;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Logging;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Threading.Tasks;
    using DeltaX.Rpc.JsonRpc.WebSocketConnection;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using DeltaX.RealTime;
    using Microsoft.Extensions.Configuration;
    using DeltaX.RealTime.Interfaces;

    public static class RtWebSocketBridgeExtenssion
    {
        public static IServiceCollection AddRealTimeWebSocketServices(
            this IServiceCollection services,
            TimeSpan? refreshInterval = default)
        {
            services.AddSingleton<WebSocketHandlerHub>();
            services.AddSingleton<IRpcWebSocketMiddleware>(serv =>
            {
                var logFactory = serv.GetService<ILoggerFactory>();
                var hub = serv.GetService<WebSocketHandlerHub>();
                var conn = serv.GetService<IRtConnector>(); 
                return new RealTimeRpcWebSocketMiddleware(conn, hub, logFactory, refreshInterval);
            }); 
            services.AddSingleton<IRpcConnection, JsonRpcWebSocketConnection>();
            services.AddSingleton<Rpc.JsonRpc.Rpc>();

            return services;
        }

        public static IApplicationBuilder UseRealTimeWebSocketBridge(this IApplicationBuilder app, string mapPrefix = "/rt")
        {
            var hub = app.ApplicationServices.GetService<WebSocketHandlerHub>();  
            // Force instance Singleton RPC 
            _ = app.ApplicationServices.GetService<Rpc.JsonRpc.Rpc>();  

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
