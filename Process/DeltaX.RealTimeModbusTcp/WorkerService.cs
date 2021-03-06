﻿ 
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


public class WorkerService : BackgroundService
{
    private readonly ModbusTcpClientService service;

    public WorkerService(ModbusTcpClientService service)
    {
        this.service = service;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return service.ExecuteAsync(stoppingToken);
    }
}
 