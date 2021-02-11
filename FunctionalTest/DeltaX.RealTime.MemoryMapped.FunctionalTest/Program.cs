namespace DeltaX.RealTime.MemoryMapped.FunctionalTest
{
    using DeltaX.Configuration;
    using DeltaX.MemoryMappedRecord;
    using DeltaX.RealTime.RtMemoryMapped;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {

        static void TaskTest()
        {
            var logger = Configuration.DefaultLoggerFactory.CreateLogger($"TaskTest_{Task.CurrentId.Value}");
             
            var config = new KeyValueMemoryConfiguration() { MemoryName = "DemoMemory"};
            var conn = RtConnectorMemoryMapped.Build(config, syncWithExistentTopics: true);

            var tag = conn.AddTag($"TagPrueba_{Task.CurrentId.Value}");

            logger.LogInformation("Previo:: Tag:{0} Status:{1} Updated:{2} Value:{3}",
                tag.TagName, tag.Status, tag.Updated.ToString("o"), tag.Value.Numeric);

            tag.SetNumeric(Task.CurrentId.Value+0.001);

            logger.LogInformation("Despues de escribir:: Tag:{0} Status:{1} Updated:{2} Value:{3}",
                tag.TagName, tag.Status, tag.Updated.ToString("o"), tag.Value.Numeric);

            while (Task.CurrentId.Value == 1)
            {
                logger.LogInformation("ALL TAGS>>>");
                foreach (var tagName in conn.TagNames)
                {
                    tag = conn.GetTag(tagName);
                    logger.LogInformation("Despues de escribir:: Tag:{0} Status:{1} Updated:{2} Value:{3}",
                        tag.TagName, tag.Status, tag.Updated.ToString("o"), tag.Value.Numeric);
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }


        static void Main(string[] args)
        {
            CommonSettings.BasePath = AppDomain.CurrentDomain.BaseDirectory;
            Configuration.SetDefaultLogger();

            string memoryName = "DemoMemory";
            int indexCapacity = 60_000_000;
            int dataCapacity = 60_000_000;
            var kvm = KeyValueMemory.Build(memoryName, indexCapacity, dataCapacity, persistent: true);
            kvm.Dispose();

            List<Task> tasks = new List<Task>();

            tasks.Add(Task.Run(TaskTest));
            tasks.Add(Task.Run(TaskTest));

            Thread.Sleep(TimeSpan.FromSeconds(3));
            tasks.Add(Task.Run(TaskTest));
            tasks.Add(Task.Run(TaskTest));
            tasks.Add(Task.Run(TaskTest));
            tasks.Add(Task.Run(TaskTest));
            tasks.Add(Task.Run(TaskTest));
            tasks.Add(Task.Run(TaskTest));
            tasks.Add(Task.Run(TaskTest));
            tasks.Add(Task.Run(TaskTest));
            tasks.Add(Task.Run(TaskTest));
            tasks.Add(Task.Run(TaskTest));
            tasks.Add(Task.Run(TaskTest));
            tasks.Add(Task.Run(TaskTest));

            Task.WaitAll(tasks.ToArray()); 

        }
    }
}
