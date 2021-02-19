using DeltaX.Modules.RealTimeRpcWebSocket;
using DeltaX.Process.UI.RtViewer;
using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; 
using Serilog;
using System;


Host
    .CreateDefaultBuilder(args)
    .UseDefaultHostBuilder(args) 
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    })
    .UseWindowsService()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<RealTimeWebSocketBridgeWorker>();
        services.AddSingleton<RtConnectorFactory>();
        services.AddSingleton<IRtConnector>(serv =>
        {
            var connFactory = serv.GetService<RtConnectorFactory>();
            var configuration = serv.GetService<IConfiguration>();
            var conn = connFactory.GetConnector(configuration.GetValue<string>("RealTimeConnectorSectionName"));
            conn.ConnectAsync();
            return conn;
        });

        services.AddRealTimeWebSocketServices(TimeSpan.FromMilliseconds(200));

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
    })
    .Build()
    .RunApp();
