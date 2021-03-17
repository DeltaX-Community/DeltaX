using DeltaX.CommonExtensions;
using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System; 
using System.Threading;
using System.Threading.Tasks;


public class WorkerService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IRtConnector connector;

    public WorkerService(
        ILogger<WorkerService> logger,
        IRtConnector connector)
    {
        _logger = logger;
        this.connector = connector;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("StopAsync at: {time}", DateTimeOffset.Now);
        return base.StopAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            connector.ConnectAsync(stoppingToken).Wait();

            _logger.LogWarning("Execution Started: {time}", DateTimeOffset.Now);

            int count = 1000;
            while (!stoppingToken.IsCancellationRequested && count-- > 0)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _logger.LogDebug("Worker running at: {time}", DateTimeOffset.Now);

                connector.SetNumeric("test/ServiceTemplateWorker", DateTime.Now.ToUnixTimestamp());
                await Task.Delay(1000, stoppingToken);
            }
        }).ContinueWith((t) =>
        {
            _logger.LogWarning("Execution Stoped: {time}", DateTimeOffset.Now);
            Environment.Exit(0);
        });
    }
}