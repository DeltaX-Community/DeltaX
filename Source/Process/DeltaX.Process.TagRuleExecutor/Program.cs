using DeltaX.Configuration;
using DeltaX.ProcessBase;
using DeltaX.RealTime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;



var host = ProcessHostBase.CreateHostBuilder(args, args, false)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<TagRuleChangeExecutorWorker>();

        services.AddSingleton<RtConnectorFactory>();
        services.AddSingleton<TagRuleChangeExecutor>();

        services.Configure<TagRuleChangeConfig>(options =>
            hostContext.Configuration.GetSection("TagRuleChangeConfig").Bind(options));
    })
    .Build();

// Configuration Logger from json
var configuration = host.Services.GetService<IConfiguration>();
Configuration.SetDefaultLogger(configuration);

var logger = host.Services.GetService<ILoggerFactory>().CreateLogger("Main"); 

try
{
    logger?.LogInformation("Start Process {process}", Process.GetCurrentProcess());
    host.Run();
}
finally
{
    logger?.LogInformation("End Process {process}", Process.GetCurrentProcess());
}

