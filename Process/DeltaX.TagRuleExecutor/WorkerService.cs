﻿using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


public class WorkerService : BackgroundService
{
    private readonly TagRuleChangeExecutorService service;

    public WorkerService(TagRuleChangeExecutorService service)
    {
        this.service = service;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return service.ExecuteAsync(stoppingToken);
    }
}

