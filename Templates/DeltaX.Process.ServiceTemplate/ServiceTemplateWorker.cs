using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


public class ServiceTemplateWorker : BackgroundService
{
    private readonly ILogger _logger;

    public ServiceTemplateWorker(ILogger<ServiceTemplateWorker> logger)
    {
        _logger = logger;
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
            _logger.LogWarning("Execution Started: {time}", DateTimeOffset.Now);

            int count = 10;
            while (!stoppingToken.IsCancellationRequested && count-- > 0)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _logger.LogDebug("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }).ContinueWith((t) =>
        {
            _logger.LogWarning("Execution Stoped: {time}", DateTimeOffset.Now);
            Environment.Exit(0);
        });
    }
}