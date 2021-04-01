namespace DeltaX.Modules.Shift
{
    using DeltaX.Modules.Shift.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Configuration;

    public static class ShiftServiceExtension
    {
        public static IHostBuilder UseShiftService(
            this IHostBuilder builder)
        {
            return builder.ConfigureServices((hostContext, services) =>
            { 
                services.AddSingleton<ShiftService>();
                services.Configure<ShfitConfiguration>(options => {
                    var cs = hostContext.Configuration.GetSection("Shfit");
                    cs.Bind(options);
                });
            });
        }
    }
}
