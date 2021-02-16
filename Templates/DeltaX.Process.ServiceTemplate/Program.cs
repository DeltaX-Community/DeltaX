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



ProcessHostBase
    .CreateHostBuilder(args, args, false)
    .UseAppConfiguration()
    .UseSerilog() 
    .UseWindowsService()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<ServiceTemplateWorker>();
        services.AddSingleton<RtConnectorFactory>();
        services.AddSingleton<IRtConnector>(services => {
            var connFactory = services.GetService<RtConnectorFactory>();
            var configuration = services.GetService<IConfiguration>();
            return connFactory.GetConnector(configuration.GetValue<string>("RealTimeConnectorSectionName"));
        });
    })
    .Build()
    .RunApp();
