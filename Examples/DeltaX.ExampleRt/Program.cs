using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

public class WorkerService : BackgroundService
{
    private readonly IRtConnector connector;

    public WorkerService(IRtConnector connector, ILogger<WorkerService> logger)
    {
        logger.LogInformation("start pepe");
        this.connector = connector;
    }


    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {

        connector.ConnectAsync().Wait();

        var pepe = connector.AddTag("pepe");

        var stats = pepe.Status;
        pepe.SetNumeric(1);

        stats = pepe.Status;

        var value = pepe.Value.Numeric;

        return Task.CompletedTask;
    }
}


class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");

        Host.CreateDefaultBuilder(args)
            .UseDefaultHostBuilder(args, false)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<WorkerService>();

                services.AddSingleton<RtConnectorFactory>();
                services.AddSingleton<IRtConnector>(services => {
                    var connFactory = services.GetService<RtConnectorFactory>();
                    var configuration = services.GetService<IConfiguration>();
                    return connFactory.GetConnector(configuration.GetValue<string>("RealTimeConnectorSectionName"));
                });
            })
            .Build()
            .RunApp();
    }
}
