using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;



Host.CreateDefaultBuilder(args)
    .UseDefaultHostBuilder(args, false)
    .UseRtConnector()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<WorkerService>(); 
        services.AddSingleton<ProcessInfoStatistics>(); 
        services.AddSingleton<TagRuleToDatabaseService>();
        services.AddSingleton<IDatabaseManager, DatabaseManager>(); 

        services.Configure<TagRuleToDatabaseConfiguration>(options =>
            hostContext.Configuration.GetSection("TagRuleToDatabase").Bind(options));
    })
    .Build()
    .RunApp();

 