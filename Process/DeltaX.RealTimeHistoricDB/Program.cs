using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;   
using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.OpenApi.Models; 
 

Host
    .CreateDefaultBuilder(args)
    .UseDefaultHostBuilder()
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    })
    .ConfigureServices((hostContext, services) =>
    {  
        services.AddHostedService<WorkerService>();

        services.AddSingleton<ProcessInfoStatistics>();
        services.AddSingleton<RtConnectorFactory>();
        services.AddSingleton<IRtConnector>(services => {
            var connFactory = services.GetService<RtConnectorFactory>();
            var configuration = services.GetService<IConfiguration>();
            return connFactory.GetConnector(configuration.GetValue<string>("RealTimeConnectorSectionName"));
        });
        services.AddSingleton<IHistoricRepository,HistoricRepositorySqlite>();
        services.AddSingleton<RealTimeHistoricDbService>();

        services.Configure<RealTimeHistoryDBConfiguration>(options =>
            hostContext.Configuration.GetSection("RealTimeHistoryDB").Bind(options));

        services.AddControllers();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "DeltaX.Process.RealTimeHistoricDB", Version = "v1" });
        });
    })    
    .Build()
    .RunApp();
