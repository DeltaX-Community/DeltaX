namespace DeltaX.Process.RealTimeHistoricDB
{
    using DeltaX.Process.RealTimeHistoricDB.Configuration;
    using DeltaX.Process.RealTimeHistoricDB.HistoryTrackerValue;
    using DeltaX.Process.RealTimeHistoricDB.Records;
    using DeltaX.Process.RealTimeHistoricDB.Repositories;
    using DeltaX.RealTime.Interfaces;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;


    public class RealTimeHistoricDbWorker : BackgroundService
    {
        private RealTimeHistoricDbService service;

        public RealTimeHistoricDbWorker(RealTimeHistoricDbService service)
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
}