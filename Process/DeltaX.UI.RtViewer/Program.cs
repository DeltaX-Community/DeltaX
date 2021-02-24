using DeltaX.Modules.RealTimeRpcWebSocket;
using DeltaX.Modules.RealTimeRpcWebSocket.Configuration;
using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using DeltaX.Rpc.JsonRpc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging; 


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
        services.AddHostedService<WorkerService>();
        services.AddSingleton<RpcHistoryService>(); 
        services.AddSingleton<Rpc>();
        services.AddSingleton(s => s.GetService<ILoggerFactory>().CreateLogger("")); 
        services.AddSingleton<RtConnectorFactory>();        
        services.AddSingleton<IRtConnector>(serv =>
        {
            var connFactory = serv.GetService<RtConnectorFactory>();
            var configuration = serv.GetService<IConfiguration>();
            var conn = connFactory.GetConnector(configuration.GetValue<string>("RealTimeConnectorSectionName"));
            conn.ConnectAsync();
            return conn;
        });

        services.Configure<RtViewConfiguration>(options =>
            hostContext.Configuration.GetSection("RtView").Bind(options));

        services.Configure<RtWebSocketBridgeConfiguration>(options =>
            hostContext.Configuration.GetSection("RtView").Bind(options));

        services.AddRealTimeWebSocketServices();

        services.AddControllers();
    })
    .Build()
    .RunApp();
