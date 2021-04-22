namespace DeltaX.Modules.Shift
{
    using DeltaX.Modules.Shift.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Configuration;
    using DeltaX.Modules.Shift.Repositories;
    using DeltaX.Modules.DapperRepository;

    public static class ShiftServiceExtension
    {
        public static IHostBuilder UseShiftService(
            this IHostBuilder builder)
        {
            DapperTypeHandler.SetDapperTypeHandler();
            return builder.ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<ShiftTableQueryFactory>();
                services.AddSingleton<IShiftRepository, ShiftRepository>();
                services.AddSingleton<IShiftService, ShiftService>();
                services.Configure<ShiftConfiguration>(options => hostContext.Configuration.GetSection("Shift").Bind(options));
            });
        }
    }
}
