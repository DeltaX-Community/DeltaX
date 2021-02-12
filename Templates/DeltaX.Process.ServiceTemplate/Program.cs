using DeltaX.Configuration; 
using DeltaX.ProcessBase;
using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;


var host = ProcessHostBase.CreateHostBuilder(args, args, false)
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
    .Build();
  

// Configuration Logger from json
var configuration = host.Services.GetService<IConfiguration>(); 
Configuration.SetDefaultLogger(configuration);

var logger = host.Services.GetService<ILoggerFactory>().CreateLogger("Main");

logger.LogInformation("CurrentDirectory: {processDirectory}", Process.GetCurrentProcess());
logger.LogDebug("CurrentDirectory: {0}", Process.GetCurrentProcess());

host.Run();
