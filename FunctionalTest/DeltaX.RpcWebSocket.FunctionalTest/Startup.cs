using DeltaX.Connections.WebSocket;
using DeltaX.Modules.RealTimeRpcWebSocket;
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
            services.AddSingleton<RtConnectorFactory>();
            services.AddSingleton<IRtConnector>(serv =>
            { 
                var connFactory = serv.GetService<RtConnectorFactory>();
                var configuration = serv.GetService<IConfiguration>();
                var conn = connFactory.GetConnector(configuration.GetValue<string>("RealTimeConnectorSectionName"));
                conn.ConnectAsync();
                return conn;
            });

            services.AddRealTimeRpcWebSocketBridge();

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

            app.UseWebSockets();
            app.UseRealTimeWebSocketBridge("/rt");
        }
    }
}