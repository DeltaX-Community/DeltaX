 
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


public class WorkerService : BackgroundService
{
    private readonly TagRuleToDatabaseService service;

    public WorkerService(TagRuleToDatabaseService service)
    {
        this.service = service;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return service.ExecuteAsync(stoppingToken);
    }
}
 