using DeltaX.Modules.RealTimeRpcWebSocket;
using DeltaX.Modules.Shift;
using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using DeltaX.Rpc.JsonRpc;
using DeltaX.Rpc.JsonRpc.Interfaces;
using DeltaX.Rpc.JsonRpc.WebSocketConnection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

Host
    .CreateDefaultBuilder(args)
    .UseDefaultHostBuilder(args, false)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    })
    .UseWindowsService()
    .UseRealTimeWebSocketServices()
    .UseShiftService()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<WorkerService>(); 
        services.AddSingleton<Rpc>(); 
        services.AddSingleton<IShiftNotification>(s => s.GetService<Rpc>()?.GetNotifyServices<IShiftNotification>());
        services.AddSingleton<ILogger>(s => s.GetService<ILoggerFactory>().CreateLogger(""));
        services.AddSingleton<RtConnectorFactory>();
        services.AddSingleton<IRtConnector>(serv =>
        {
            var connFactory = serv.GetService<RtConnectorFactory>();
            var configuration = serv.GetService<IConfiguration>();
            var conn = connFactory.GetConnector(configuration.GetValue<string>("RealTimeConnectorSectionName"));
            conn.ConnectAsync();
            return conn;
        });

        services.Configure<UIConfiguration>(options =>
            hostContext.Configuration.GetSection("UIService").Bind(options));

        services.AddControllers();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "DeltaX.UI.Server", Version = "v1" });
        });
    })
    .Build()
    .RunApp();