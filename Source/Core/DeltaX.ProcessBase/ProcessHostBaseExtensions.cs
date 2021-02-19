namespace Microsoft.Extensions.Hosting
{
    using DeltaX.Configuration;
    using DeltaX.ProcessBase;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using System;
    using System.Diagnostics;
    using System.IO;

    public static class ProcessHostBaseExtensions
    {
        static Process process = Process.GetCurrentProcess();

        public static IHostBuilder UseAppConfiguration(
            this IHostBuilder builder, 
            string[] jsonFiles = null)
        {
            return builder.ConfigureAppConfiguration(confBuilder =>
                {
                    confBuilder.AddJsonFile(CommonSettings.CommonConfigName, optional: true);
                    confBuilder.AddJsonFile("appsettings.json", optional: true);
                    var processConfigName = CommonSettings.GetProcesConfigName();
                    confBuilder.AddJsonFile(CommonSettings.GetPathConfigFile(processConfigName) ?? processConfigName, optional: true);

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

        public static void RunApp(
            this IHost host)
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

        public static IHostBuilder UseDefaultHostBuilder(
            this IHostBuilder builder, 
            string[] jsonFiles = null,
            bool pressKeyToContinue = true,
            bool showInstallHelper = true)
        {
            if (showInstallHelper)
            {
                ProcessHostBase.ShowInstallHelper(pressKeyToContinue);
            }

            Directory.SetCurrentDirectory(ProcessHostBase.ProcessDirectory);

            return builder
                .UseAppConfiguration(jsonFiles)
                .UseSerilog()
                .UseWindowsService()
                .UseContentRoot(ProcessHostBase.ProcessDirectory);
        }
    }
}
