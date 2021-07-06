using DeltaX.Modules.RealTimeRpcWebSocket;
using DeltaX.Modules.Shift;
using DeltaX.Modules.Shift.Shared;
using DeltaX.RealTime; 
using DeltaX.Rpc.JsonRpc;
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
    .UseRtConnectorService()
    .UseRealTimeWebSocketServices()
    .UseShiftService()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<WorkerService>(); 
        services.AddSingleton<Rpc>(); 
        services.AddSingleton<IShiftNotification>(s => s.GetService<Rpc>()?.GetNotifyServices<IShiftNotification>());
        services.AddSingleton<ILogger>(s => s.GetService<ILoggerFactory>().CreateLogger("")); 

        services.Configure<UIConfiguration>(options => hostContext.Configuration.GetSection("UIService").Bind(options));

        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new TimeSpanToStringConverter()));

        services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "DeltaX.UI.Server", Version = "v1" }));
    })
    .Build()
    .RunApp();