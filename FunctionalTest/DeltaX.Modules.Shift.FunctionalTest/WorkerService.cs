using DeltaX.Modules.Shift;
using DeltaX.Modules.Shift.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

public class WorkerService : BackgroundService
{
    private readonly IShiftService shiftService;
    private readonly ShiftConfiguration configuration; 
    private readonly ILogger logger;

    public WorkerService(IShiftService shiftService, IOptions<ShiftConfiguration> options, ILogger<WorkerService> logger)
    {
        this.shiftService = shiftService;
        this.configuration = options.Value;
        this.logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var taskService = shiftService.ExecuteAsync(stoppingToken)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Console.WriteLine("FATAL ERROR {0}", t.Exception);
                    Environment.Exit(-1);
                }
            });

        var taskTest = Task.Run( async() =>
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));

                foreach (var p in configuration.ShiftProfiles)
                {
                    try
                    {
                        var r = shiftService.GetShiftCrew(p.Name, DateTime.Now); 
                        logger.LogDebug("Current Shift {@r}", r);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Ni idea");
                    }
                }
            }
        });

        return Task.WhenAll(taskService, taskTest);

        //DateTime date = DateTime.Parse("2021-03-26T00:00:00-03:00");
        //var shift = service.GetShift(date);
        //Assert.IsNull(shift);

        //date = DateTime.Parse("2021-03-26T06:00:00-03:00");
        //shift = service.GetShift(date);
        //Assert.AreEqual("Mañana", shift.Name); 

        //date = DateTime.Parse("2021-03-26T12:00:00-03:00");
        //shift = service.GetShift(date);
        //Assert.AreEqual("Tarde", shift.Name);

        //date = DateTime.Parse("2021-03-26T18:00:00-03:00");
        //shift = service.GetShift(date);
        //Assert.AreEqual("Noche", shift.Name); 

        //date = DateTime.Parse("2021-03-27T00:00:00-03:00");
        //shift = service.GetShift(date);
        //Assert.IsNull(shift);

        //// sabado. igual es turno valido, pero no hay escuadra
        //date = DateTime.Parse("2021-03-27T06:00:00-03:00");
        //shift = service.GetShift(date);
        //Assert.AreEqual("Mañana", shift.Name);

        //// Escudras/Cuadrillas
        //date = DateTime.Parse("2021-03-26T05:59:59-03:00");
        //var crew = service.GetCrew(date);
        //Assert.IsNull(crew);

        //date = DateTime.Parse("2021-03-26T06:00:00-03:00");
        //crew = service.GetCrew(date);
        //Assert.AreEqual("A", crew.Name);

        //date = DateTime.Parse("2021-03-26T11:59:59-03:00");
        //crew = service.GetCrew(date);
        //Assert.AreEqual("A", crew.Name);

        //date = DateTime.Parse("2021-03-26T12:00:00-03:00");
        //crew = service.GetCrew(date);
        //Assert.AreEqual("B", crew.Name);

        //date = DateTime.Parse("2021-03-26T18:00:00-03:00");
        //crew = service.GetCrew(date);
        //Assert.AreEqual("C", crew.Name);

        //date = DateTime.Parse("2021-03-26T23:59:59-03:00");
        //crew = service.GetCrew(date);
        //Assert.AreEqual("C", crew.Name);

        //date = DateTime.Parse("2021-03-27T00:00:00-03:00");
        //crew = service.GetCrew(date);
        //Assert.IsNull(crew);

        //// holiday
        //date = DateTime.Parse("2021-03-24T12:00:00-03:00");
        //crew = service.GetCrew(date);
        //Assert.IsNull(crew);

        // return Task.CompletedTask;
    }
}
 