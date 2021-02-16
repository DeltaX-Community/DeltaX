using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;  
using Serilog;
using DeltaX.ProcessBase;
using DeltaX.Configuration;
using DeltaX.Process.RealTimeHistoricDB;
using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using DeltaX.Process.RealTimeHistoricDB.Repositories;
using DeltaX.Process.RealTimeHistoricDB.Configuration;
using System.IO;

ProcessHostBase.ShowInstallHelper(false); 
Directory.SetCurrentDirectory(ProcessHostBase.ProcessDirectory); 

Host
    .CreateDefaultBuilder(args)
    .UseAppConfiguration() 
    .UseSerilog()
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    })
    .UseWindowsService()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<RealTimeHistoricDbWorker>();
        services.AddSingleton<RtConnectorFactory>();
        services.AddSingleton<IRtConnector>(services => {
            var connFactory = services.GetService<RtConnectorFactory>();
            var configuration = services.GetService<IConfiguration>();
            return connFactory.GetConnector(configuration.GetValue<string>("RealTimeConnectorSectionName"));
        });
        services.AddSingleton<IHistoricRepository,HistoricRepositorySqlite>();
        services.AddSingleton<RealTimeHistoricDbService>();

        services.Configure<RealTimeHistoryDBConfiguration>(options =>
            hostContext.Configuration.GetSection("RealTimeHistoryDBConfiguration").Bind(options));
    })
    .Build()
    .RunApp();
