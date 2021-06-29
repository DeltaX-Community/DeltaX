using DeltaX.Modules.Shift;
using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using DeltaX.Rpc.JsonRpc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Host.CreateDefaultBuilder(args)
    .UseDefaultHostBuilder(args, false)
    .UseShiftService()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton<RtConnectorFactory>();
        services.AddSingleton<IRtConnector>(serv => serv.GetService<RtConnectorFactory>().GetDefaultConnector());
        services.AddHostedService<WorkerService>();
        
        services.AddSingleton<IShiftNotification>(serv => serv.GetService<Rpc>()?.GetNotifyServices<IShiftNotification>());
    })
    .Build()
    .RunApp();