using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging; 
using System.Diagnostics;
using Serilog;
using DeltaX.ProcessBase;
using DeltaX.Configuration;
using DeltaX.RpcWebSocket.FunctionalTest;


Host
    .CreateDefaultBuilder(args)
    .UseAppConfiguration()
    .UseSerilog()
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    })
    // .UseWindowsService()
    // .ConfigureServices((hostContext, services) =>
    // {
    //     services.AddHostedService<WebSocketsWorker>(); 
    // })
    .Build() 
    .RunApp();  