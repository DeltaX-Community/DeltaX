using DeltaX.Connections.WebSocket;
using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using DeltaX.Rpc.JsonRpc.Interfaces;
using DeltaX.Rpc.JsonRpc.WebSocketConnection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace DeltaX.RpcWebSocket.FunctionalTest
{
    public class Startup
    { 
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<WebSocketHandlerHub>();
            services.AddSingleton<RtConnectorFactory>();
           
            services.AddSingleton<IRtConnector>(services => {
                var connFactory = services.GetService<RtConnectorFactory>();
                var configuration = services.GetService<IConfiguration>();
                var conn =  connFactory.GetConnector(configuration.GetValue<string>("RealTimeConnectorSectionName"));
                conn.ConnectAsync();
                return conn;
            });
            services.AddSingleton<RealTimeRpcWebSocketMiddleware>();
            services.AddSingleton<IRpcWebSocketMiddleware>(s => s.GetService<RealTimeRpcWebSocketMiddleware>());
            services.AddSingleton<IRpcConnection, JsonRpcWebSocketConnection>();
            services.AddSingleton<Rpc.JsonRpc.Rpc>(); 

            services.AddControllers();

            services.AddCors(options =>
            {
                options.AddPolicy("DefaultVueCors", builder =>
                {
                    builder
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()
                      .WithOrigins("http://127.0.0.1:8080", "https://127.0.0.1:8081", "http://localhost:8080");
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
           

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("DefaultVueCors");

            // serve wwwroot
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseFileServer();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseRtWebSocketBridge("/ws");
        }

        // public void ConfigureWebSocket(IApplicationBuilder app)
        // {
        //    var  hub = app.ApplicationServices.GetService<WebSocketHandlerHub>();
        // 
        //     app.Use(async (context, next) =>
        //     {
        //         if (context.WebSockets.IsWebSocketRequest)
        //         {
        //             var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        //             await hub.RegisterWebSocket(webSocket).ReceiveAsync();
        //             return;
        //         }
        //         await next();
        //     });
        // }
    }

    public static class WsExtend
    {
        public static IApplicationBuilder UseRtWebSocketBridge(this IApplicationBuilder app, string mapPrefix = "/rt")
        {
            var hub = app.ApplicationServices.GetService<WebSocketHandlerHub>(); 
            var rt = app.ApplicationServices.GetService<RealTimeRpcWebSocketMiddleware>();
            var rpc = app.ApplicationServices.GetService<Rpc.JsonRpc.Rpc>();
            var logger = app.ApplicationServices.GetService<ILogger<RealTimeRpcWebSocketMiddleware>>();

            Task.Run(async () =>
            { 
                logger.LogWarning("Execution Started: {time}", DateTimeOffset.Now);

                while (true)
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    logger.LogInformation("clients {clients}", hub.GetClients().Count());
                     
                    rt.ForceRefreshTags();
                    await Task.Delay(1000);
                }
            }).ContinueWith((t) =>
            {
                logger.LogWarning("Execution Stoped: {time}", DateTimeOffset.Now);
            });

            return app
                .UseWebSockets()
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
