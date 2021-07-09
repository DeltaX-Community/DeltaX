namespace DeltaX.Modules.Shift
{
    using DeltaX.Modules.Shift.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Configuration;
    using DeltaX.Modules.Shift.Repositories;
    using DeltaX.Modules.DapperRepository;
    using DeltaX.Modules.Shift.Shared;
    using DeltaX.Database;
    using Microsoft.Extensions.Options;
    using System.Data;

    public static class ShiftServiceExtension
    {
        public static IHostBuilder UseShiftService(this IHostBuilder builder, string sectionName = "Shift")
        {
            DapperTypeHandler.SetDapperTypeHandler();
            return builder.ConfigureServices((hostContext, services) =>
            {
                services.Configure<ShiftConfiguration>(options => hostContext.Configuration.GetSection(sectionName).Bind(options));
                services.AddShiftService();
            });
        }

        public static IServiceCollection AddShiftService(this IServiceCollection services)
        {
            services.AddSingleton<ShiftTableQueryFactory>(serv =>
            {
                var config = serv.GetService<IOptions<ShiftConfiguration>>();
                return new ShiftTableQueryFactory(config.Value.DatabaseDialectType);
            });
            services.AddSingleton<DatabaseManager>();
            services.AddSingleton<IDatabaseBase>(serv =>
            {
                var config = serv.GetService<IOptions<ShiftConfiguration>>();
                return serv.GetService<DatabaseManager>().GetDatabase(config.Value.DatabaseConnectionFactory);
            });
            services.AddScoped<IDbConnection>(serv => serv.GetService<IDatabaseBase>().GetConnection());
            services.AddScoped<IShiftUnitOfWork, ShiftUnitOfWork>();
            services.AddScoped<IShiftRepository, ShiftRepository>();
            services.AddScoped<ShiftServiceScoped>();
            services.AddSingleton<IShiftService, ShiftService>();

            return services;
        }
    }
}
