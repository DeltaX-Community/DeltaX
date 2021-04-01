using DeltaX.Modules.Shift;
using DeltaX.Modules.Shift.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

Host.CreateDefaultBuilder(args)
    .UseDefaultHostBuilder(args, false)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<WorkerService>();
        services.AddSingleton<ShiftService>();
        services.Configure<ShfitConfiguration>(options => hostContext.Configuration.GetSection("Shfit").Bind(options));
    })
    .Build()
    .RunApp();