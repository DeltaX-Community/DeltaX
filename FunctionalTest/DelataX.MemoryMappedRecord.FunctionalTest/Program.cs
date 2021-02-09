using DeltaX.CommonExtensions;
using DeltaX.Configuration;
using DeltaX.MemoryMappedRecord;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace DelataX.MemoryMappedRecord.FunctionalTest
{
    class Program
    {
        static void Main(string[] args)
        {
            CommonSettings.SetBasePathFromExecutable();

            Configuration.SetDefaultLogger();
            var logger = Configuration.DefaultLogger;

            int rows = 200_000;
            string memoryName = "DemoMemory";
            int indexCapacity = 60_000_000;
            int dataCapacity = 60_000_000;

            var kvm = new KeyValueMemory(memoryName, indexCapacity, dataCapacity, persistent: true);

            var key = "KeyPrueba_3";
            var value = ("Hola mundo como estas: " + DateTime.Now).GetBytes();
            // kvm.AddValue(key, value);

            Console.WriteLine("Pres W for WRITE values only");
            var consoleKey = Console.ReadKey();

            var loopWrite = 10;
            while (--loopWrite > 0 || consoleKey.Key == ConsoleKey.W)
            {
                int writed = 0;
                var start = DateTime.Now;
                for (var i = 0; i < rows; i++)
                {
                    key = "Key_" + i;
                    var val = ("Hola mundo " + i + " como estas: " + DateTime.Now).GetBytes();
                    kvm.SetValue(key, val, true);

                    writed += val.Length;
                }

                var diff = DateTime.Now - start;
                var rps = kvm.Keys.Count / (diff.TotalSeconds);
                var rpsr = writed / (diff.TotalSeconds);

                logger.LogInformation("--- Write rows:{0} Time:{1} Freq:[{2}]",
                    kvm.Count, CommonExtensions.SizeSuffix(diff.TotalSeconds, 3, 1000), $"{CommonExtensions.SizeSuffix(rps, 1, 1000)}QPS");
                logger.LogInformation("--- Write [{0}]", $"{CommonExtensions.SizeSuffix(rpsr, 1, 1000)}BPS");
            }

            Console.WriteLine("Pres key to READ values");
            Console.ReadLine();

            var loopIndex = 0;
            while (true)
            {
                int reader = 0;
                var start = DateTime.Now;
                double updated = 0;

                foreach (var k in kvm.Keys)
                {
                    key = k;
                    value = kvm.GetValue(k);
                    updated = kvm.GetUpdated(k);

                    reader += value.Length;

                    // Imprime el valor para demostrar funcionamiento
                    if (key == $"Key_{loopIndex}")
                    {

                        logger.LogInformation("Read Key:{key} value:{value} update:{update}",
                            key, value.GetString(), updated.FromUnixTimestamp().ToString("o"));
                    }
                }

                var diff = DateTime.Now - start;
                var rps = kvm.Count / (diff.TotalSeconds);
                var rpsr = reader / (diff.TotalSeconds);

                logger.LogInformation("--- Read  kvm.Count:{0} Time Diff:{1} Freq: [{2}]",
                    kvm.Count, diff.TotalSeconds, $"{CommonExtensions.SizeSuffix(rps, 1, 1000)}QPS");
                logger.LogInformation("--- Read [{0}]", $"{CommonExtensions.SizeSuffix(rpsr, 1, 1000)}BPS");

                Thread.Sleep(20);
                loopIndex++;
            }
        }
    }
}