using DeltaX.Modules.Shift;
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
    private readonly SimulationService simulation;
    private readonly RpcHistoryService historyService;
    private readonly IShiftService shiftService;
    private readonly Rpc rpc;

    public WorkerService(
        ILogger<WorkerService> logger, 
        SimulationService simulation,
        RpcHistoryService historyService,
        IShiftService shiftService,
        Rpc rpc)
    {
        this.logger = logger; 
        this.simulation = simulation;
        this.historyService = historyService;
        this.shiftService = shiftService;
        this.rpc = rpc;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {  
        rpc.Dispatcher.RegisterService<IShiftService>(shiftService, "shift.");
        rpc.Dispatcher.RegisterMethodAlias(historyService, "rpc.history.get_topic", nameof(historyService.GetTopicHistory));
        rpc.UpdateRegisteredMethods();

        var srv1 = shiftService.RunAsync(stoppingToken);
        var srv2 = rpc.RunAsync(stoppingToken);
        var srv3 = simulation.RunAsync(stoppingToken);

        return Task.WhenAny(srv1, srv2, srv3).ContinueWith((t) =>
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