namespace DeltaX.RealTime
{
    using DeltaX.RealTime.Interfaces;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public static class RtConnectorExtension
    {
        public static IHostBuilder UseRtConnector(this IHostBuilder builder, string realTimeConnectorSectionName = "RealTimeConnectorSectionName")
        { 
            return builder.ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<RtConnectorFactory>();
                services.AddSingleton<IRtConnector>(serv =>
                {
                    var connFactory = serv.GetService<RtConnectorFactory>(); 
                    var conn = connFactory.GetConnector(hostContext.Configuration.GetValue<string>(realTimeConnectorSectionName));
                    conn.ConnectAsync();
                    return conn;
                });
            });
        }
    }
}
