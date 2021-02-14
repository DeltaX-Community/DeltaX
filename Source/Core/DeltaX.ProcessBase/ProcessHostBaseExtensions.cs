namespace Microsoft.Extensions.Hosting
{
    using DeltaX.Configuration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging; 
    using System;
    using System.Diagnostics;
    using System.IO;

    public static class ProcessHostBaseExtensions
    {
        static Process process = Process.GetCurrentProcess();

        public static IHostBuilder UseAppConfiguration(this IHostBuilder hostBuilder, string[] jsonFiles = null)
        {
            CommonSettings.BasePath = @"D:\DEV\repos\DeltaX-Community\DeltaX";

            return hostBuilder.ConfigureAppConfiguration(confBuilder =>
                {
                    confBuilder.AddJsonFile(CommonSettings.CommonConfigName, optional: true);
                    confBuilder.AddJsonFile("appsettings.json", optional: true);
                    confBuilder.AddJsonFile(CommonSettings.GetProcesConfigName(), optional: true);

                    if (jsonFiles != null)
                    {
                        foreach (var fileName in jsonFiles)
                        {
                            if (File.Exists(fileName))
                            {
                                Console.WriteLine("AddJsonFile: {0}", fileName);
                                confBuilder.AddJsonFile(fileName, optional: true);
                            }
                        }
                    }
                });
        }

        public static void RunApp(this IHost host)
        {
            var configuration = host.Services.GetService<IConfiguration>();
            Configuration.SetDefaultLogger(configuration);
            var logger = host.Services.GetService<ILoggerFactory>().CreateLogger("RunApp");

            try
            {
                logger.LogInformation("Process Started: {process}", process.MainModule.FileName);
                host.Run();
            }
            finally
            {
                logger.LogInformation("Process Finished {process}", process.MainModule.FileName);
            }
        }
    }
}
