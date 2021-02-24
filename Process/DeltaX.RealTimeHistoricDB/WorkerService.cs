using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;


public class WorkerService : BackgroundService
{
    private RealTimeHistoricDbService service;

    public WorkerService(RealTimeHistoricDbService service)
    {
        this.service = service;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        service.CreateTables();
        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return service.ExecuteAsync(stoppingToken);
    }
}