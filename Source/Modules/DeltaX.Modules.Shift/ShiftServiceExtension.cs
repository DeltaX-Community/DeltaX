namespace DeltaX.Modules.Shift
{
    using DeltaX.Modules.Shift.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Configuration;
    using DeltaX.Modules.Shift.Repositories;
    using DeltaX.Modules.DapperRepository;
    using DeltaX.Database;
    using Microsoft.Extensions.Options;
    using MySqlConnector;

    public static class ShiftServiceExtension
    {
        public static IHostBuilder UseShiftService(
            this IHostBuilder builder)
        {
            DapperTypeHandler.SetDapperTypeHandler();
            return builder.ConfigureServices((hostContext, services) =>
            { 
                services.AddSingleton<ShiftTableQueryFactory>();
                services.AddScoped<IShiftUnitOfWork, ShiftUnitOfWork>();
                services.AddScoped<IShiftRepository, ShiftRepository>();
                services.AddSingleton<IShiftService, ShiftService>();
                services.Configure<ShiftConfiguration>(options => hostContext.Configuration.GetSection("Shift").Bind(options));
           //   services.AddTransient<IShiftRepository>(srv =>
           //   {
           //       var options = srv.GetService<IOptions<ShiftConfiguration>>();
           //       var dbFactory = new DbConnectionFactory(typeof(MySqlConnection), new[] { options.Value.ConnectionString });
           //       var conn = dbFactory.GetConnection();
           //
           //       return new ShiftRepository();
           //   });
            });
        }
    }
}
