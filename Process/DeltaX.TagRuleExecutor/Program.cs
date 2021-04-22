using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; 


Host.CreateDefaultBuilder(args) 
    .UseDefaultHostBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<WorkerService>();

        services.AddSingleton<ProcessInfoStatistics>();
        services.AddSingleton<RtConnectorFactory>();
        services.AddSingleton<TagRuleChangeExecutorService>();
        services.AddSingleton<IRtConnector>(services => services.GetService<RtConnectorFactory>().GetDefaultConnector());

        services.Configure<TagRuleExecutorConfiguration>(options =>
            hostContext.Configuration.GetSection("TagRuleExecutor").Bind(options));
    })
    .Build()
    .RunApp(); 
