namespace DeltaX.ProcessBase
{
    using DeltaX.Configuration;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using System;
    using System.Diagnostics;
    using System.IO;

    public class ProcessHostBase
    {
        static Process process = Process.GetCurrentProcess();
        public static string ProcessDirectory = Path.GetDirectoryName(process.MainModule.FileName);

        public static void ShowInstallHelper(bool pressKeyToContinue = true)
        {
            if (Environment.UserInteractive && CommonSettings.IsWindowsOs)
            {
                string servName = process.ProcessName;
                string binPath = process.MainModule.FileName;
                string description = "Real Time Tag Rule Executor";

                Console.WriteLine("Running {0} in Console Mode...", process.ProcessName);
                Console.WriteLine("\nHelp for install on cmd as Administrator:");
                Console.WriteLine($" - install service:     \n    sc create {servName} binpath=\"{binPath}\" ");
                Console.WriteLine($" - uninstall service:   \n    sc delete {servName} ");
                Console.WriteLine($" - description service: \n    sc description {servName} \"{description}\"");
                if (pressKeyToContinue)
                {
                    Console.WriteLine("\nPress key to continue");
                    Console.ReadLine();
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(
            string[] args,
            string[] jsonFiles = null,
            bool pressKeyToContinue = true,
            bool showInstallHelper = true)
        {
            if (showInstallHelper)
            {
                ShowInstallHelper(pressKeyToContinue);
            }

            Directory.SetCurrentDirectory(ProcessDirectory);
            CommonSettings.BasePath = @"D:\DEV\repos\DeltaX-Community\DeltaX";

            return Host.CreateDefaultBuilder(args) 
                .UseAppConfiguration(jsonFiles)
                .UseSerilog()
                .UseWindowsService()
                .UseContentRoot(ProcessDirectory);
        }
    }
}
