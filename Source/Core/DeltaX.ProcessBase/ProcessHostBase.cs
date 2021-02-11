using DeltaX.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;

namespace DeltaX.ProcessBase
{
    public class ProcessHostBase
    {
        static Process process = Process.GetCurrentProcess();
        static string processDirectory = Path.GetDirectoryName(process.MainModule.FileName);

        public static void ShowInstallHelper(bool pressKeyToContinue = true)
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

        public static IHostBuilder CreateHostBuilder(string[] args, string[] jsonFiles = null, bool pressKeyToContinue = true)
        {
            if (Environment.UserInteractive && CommonSettings.IsWindowsOs)
            {
                ShowInstallHelper(pressKeyToContinue);
            }

            Directory.SetCurrentDirectory(processDirectory);
            CommonSettings.BasePath = @"D:\DEV\repos\DeltaX-Community\DeltaX";

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(confBuilder =>
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
                })
                .UseSerilog()
                .UseWindowsService()
                .UseContentRoot(processDirectory);
        }
    }
}
