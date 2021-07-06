using DeltaX.Modules.Shift;
using DeltaX.Modules.Shift.Shared.Dtos;
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
    private readonly IShiftService shiftService;
    private readonly IRtConnector connector;
    // private readonly RpcHistoryService historyService;
    private readonly Rpc rpc;

    public WorkerService(
        ILogger<WorkerService> logger,
        IRpcConnection rpcConnection,
        IShiftService shiftService,
        IRtConnector connector, 
        Rpc rpc)
    {
        this.logger = logger;
        this.rpcConnection = rpcConnection;
        this.shiftService = shiftService;
        this.connector = connector; 
        this.rpc = rpc;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {  
        rpc.Dispatcher.RegisterService<IShiftService>(shiftService, "shift");
        rpc.UpdateRegisteredMethods();

        var srv1 = shiftService.RunAsync(stoppingToken);
        var srv2 = rpcConnection.RunAsync(stoppingToken);

        return Task.WhenAny(srv1, srv2).ContinueWith((t) =>
        {
            if (t.Result.IsFaulted)
            {
                logger.LogError("Execution Stoped: {time} {error}", DateTimeOffset.Now, t.Result.Exception);
                Environment.Exit(-1);
            }
            else
            {
                logger.LogWarning("Execution Stoped: {time}", DateTimeOffset.Now);
            }
        });
    }
}