using DeltaX.Configuration;
using DeltaX.RealTime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; 
using Serilog;
using System;
using System.Diagnostics;
using System.IO;


var process = Process.GetCurrentProcess();
var processDirectory = Path.GetDirectoryName(process.MainModule.FileName);

IHostBuilder CreateHostBuilder(string[] args) => Host
    .CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(confBuilder =>
    { 
        confBuilder.AddJsonFile(CommonSettings.GetProcesConfigName(), optional: true);
        foreach (var fileName in args)
        {
            if (File.Exists(fileName))
            {
                Console.WriteLine("AddJsonFile: {0}", fileName);
                confBuilder.AddJsonFile(fileName, optional: true);
            }
        }
    })
    .UseSerilog()
    .UseWindowsService()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<TagRuleChangeExecutorWorker>();

        services.AddSingleton<RtConnectorFactory>();
        services.AddSingleton<TagRuleChangeExecutor>();

        services.Configure<TagRuleChangeConfig>(options =>
            hostContext.Configuration.GetSection("TagRuleChangeConfig").Bind(options));
    });


void ShowInstall()
{ 
    string servName = process.ProcessName;
    string binPath = process.MainModule.FileName;
    string description = "Real Time Tag Rule Executor";

    Console.WriteLine("Running {0} in Console Mode...", process.ProcessName);
    Console.WriteLine("\nHelp for install on cmd as Administrator:");
    Console.WriteLine($" - install service:     \n    sc create {servName} binpath=\"{binPath}\" ");
    Console.WriteLine($" - uninstall service:   \n    sc delete {servName} ");
    Console.WriteLine($" - description service: \n    sc description {servName} \"{description}\"");
    Console.WriteLine("\nPress key to continue");
    Console.ReadLine();
}


if (Environment.UserInteractive && CommonSettings.IsWindowsOs)
{
    ShowInstall(); 
}

Console.WriteLine(processDirectory);
Directory.SetCurrentDirectory(processDirectory);
 
var host = CreateHostBuilder(args)
    .UseContentRoot(processDirectory)
    .Build();

// Configuration Logger from json
var configuration = host.Services.GetService<IConfiguration>();
var logConfig = DeltaX.Configuration.Serilog.LoggerConfiguration.GetSerilogConfiguration();
logConfig.ReadFrom.Configuration(configuration);
Log.Logger = logConfig.CreateLogger();
Log.Logger.Information("CurrentDirectory: {processDirectory}", processDirectory);

host.Run();



