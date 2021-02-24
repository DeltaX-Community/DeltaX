using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using DeltaX.RpcWebSocket.FunctionalTest;


Host
    .CreateDefaultBuilder(args)
    .UseAppConfiguration()
    .UseSerilog()
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    })
    .UseWindowsService()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<RealTimeWebSocketBridgeWorker>();
    })
    .Build()
    .RunApp();