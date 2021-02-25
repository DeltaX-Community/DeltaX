using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;



Host.CreateDefaultBuilder(args)
    .UseDefaultHostBuilder(args, false)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<WorkerService>();

        services.AddSingleton<ProcessInfoStatistics>();
        services.AddSingleton<RtConnectorFactory>();
        services.AddSingleton<TagRuleToDatabaseService>();
        services.AddSingleton<IDatabaseManager, DatabaseManager>();

        services.AddSingleton<IRtConnector>(services => {
            var connFactory = services.GetService<RtConnectorFactory>();
            var configuration = services.GetService<IConfiguration>();
            return connFactory.GetConnector(configuration.GetValue<string>("RealTimeConnectorSectionName"));
        });

        services.Configure<TagRuleToDatabaseConfiguration>(options =>
            hostContext.Configuration.GetSection("TagRuleToDatabase").Bind(options));
    })
    .Build()
    .RunApp();

 