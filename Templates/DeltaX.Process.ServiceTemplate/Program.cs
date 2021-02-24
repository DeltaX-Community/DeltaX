using DeltaX.Configuration; 
using DeltaX.ProcessBase;
using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Diagnostics;

 
Host.CreateDefaultBuilder(args)
    .UseDefaultHostBuilder(args, false) 
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<WorkerService>();
        services.AddSingleton<RtConnectorFactory>();
        services.AddSingleton<IRtConnector>(services => {
            var connFactory = services.GetService<RtConnectorFactory>();
            var configuration = services.GetService<IConfiguration>();
            return connFactory.GetConnector(configuration.GetValue<string>("RealTimeConnectorSectionName"));
        });
    })
    .Build()
    .RunApp();
