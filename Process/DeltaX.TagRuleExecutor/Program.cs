using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; 


Host.CreateDefaultBuilder(args) 
    .UseDefaultHostBuilder(args)
    .UseRtConnector()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<WorkerService>(); 
        services.AddSingleton<ProcessInfoStatistics>(); 
        services.AddSingleton<TagRuleChangeExecutorService>(); 

        services.Configure<TagRuleExecutorConfiguration>(options =>
            hostContext.Configuration.GetSection("TagRuleExecutor").Bind(options));
    })
    .Build()
    .RunApp(); 
