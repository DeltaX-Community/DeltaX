using DeltaX.RealTime.Interfaces;
using DeltaX.Rpc.JsonRpc;
using DeltaX.Rpc.JsonRpc.Interfaces; 
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;


public class WorkerService : BackgroundService
{
    private readonly ILogger logger;
    private readonly IRpcConnection rpcConnection;
    private readonly IRtConnector connector;
    private readonly RpcHistoryService historyService;
    private readonly Rpc rpc;

    public WorkerService(
        ILogger<WorkerService> logger,
        IRpcConnection rpcConnection,
        IRtConnector connector,
        RpcHistoryService historyService,
        Rpc rpc)
    {
        this.logger = logger;
        this.rpcConnection = rpcConnection;
        this.connector = connector;
        this.historyService = historyService;
        this.rpc = rpc;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        rpc.Dispatcher.RegisterMethodAlias(
            historyService,
            "rpc.history.get_topic",
            nameof(historyService.GetTopicHistory));

        rpc.UpdateRegisteredMethods();

        return rpcConnection
            .ConnectAsync(stoppingToken)
            .ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    logger.LogError("Execution RealTimeWebSocketBridgeWorker Stoped: {time} {error}", DateTimeOffset.Now, t.Exception);
                    Environment.Exit(-1);
                }
                else
                {
                    logger.LogWarning("Execution RealTimeWebSocketBridgeWorker Stoped: {time}", DateTimeOffset.Now);
                }
            });
    }
}