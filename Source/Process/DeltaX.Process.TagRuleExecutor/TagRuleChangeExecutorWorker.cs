using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


public class TagRuleChangeExecutorWorker : BackgroundService
{
    private readonly ILogger<TagRuleChangeExecutorWorker> logger;
    private readonly RtConnectorFactory connectorFactory;
    private readonly TagRuleChangeExecutor executor;
    private readonly IRtConnector connector;
    private readonly TagRuleChangeConfig settings; 

    public TagRuleChangeExecutorWorker(
        ILogger<TagRuleChangeExecutorWorker> logger,
        RtConnectorFactory connectorFactory,
        TagRuleChangeExecutor executor,
        IOptions<TagRuleChangeConfig> settings)
    {
        this.logger = logger;
        this.connector = connectorFactory.GetConnector(settings?.Value?.RealTimeConnectorSectionName);
        this.connectorFactory = connectorFactory;
        this.executor = executor;
        this.settings = settings.Value;        
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogWarning("StopAsync at: {time}", DateTimeOffset.Now.ToString("o"));
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            this.executor.LoadRules();
            var LoopEvaluateInterval = settings?.LoopEvaluateIntervalMilliseconds ?? 500;

            await this.executor.ConnectAsync(stoppingToken);

            var t1 = Task.Run(async () =>
            {
                await this.connector.ConnectAsync(stoppingToken);

                int count = 0;
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                while (count < 100 && !stoppingToken.IsCancellationRequested)
                {
                    connector.SetNumeric("tag1", count++);
                    connector.SetNumeric("tag2/Task.CurrentId", Task.CurrentId ?? 1);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }, stoppingToken);

            var t2 = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogDebug("- Worker running at: {time}", DateTimeOffset.Now.ToString("o"));
                    executor.EvaluateChanges();
                    await Task.Delay(TimeSpan.FromMilliseconds(LoopEvaluateInterval), stoppingToken);
                }
            });

            await Task.WhenAll(t1, t2);
        }
        finally
        {
            logger.LogWarning("Process Stoped at: {time}", DateTimeOffset.Now.ToString("o"));
        }
    }
}

