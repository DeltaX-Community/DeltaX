namespace DeltaX.Rpc.JsonRpc.Mqtt.FunctionalTest.Server
{
    using DeltaX.Configuration.Serilog;
    using DeltaX.Rpc.JsonRpc.Mqtt.FunctionalTest.Shared;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class ExampleService : IExampleService
    {
        private readonly Rpc rpc;
        private readonly ILogger logger;

        public ExampleService(Rpc rpc)
        {
            this.rpc = rpc;
            this.logger = LoggerConfiguration.DefaultLoggerFactory.CreateLogger("Service");
        }

        public async Task SendBroadcastAsync(object message)
        {
            logger.LogInformation("*** SendBroadcastAsync Receive: {@0}", message);
            await Task.Delay(2000);
            await rpc.NotifyAsync(nameof(INotifications.NotificationBroadcast), message);

            logger.LogInformation("*** SendBroadcastAsync END");
        }

        public string Concatenar(string format, int a, int b)
        {
            return $"Formated format:{format} a:{a} b:{b}";
        }

        public int Sumar(int a, int b)
        {
            var result = a + b;
            rpc.NotifyAsync(nameof(INotifications.NotifySumar), result);
            return result;
        }

        public void TaskVoid(int a, int b)
        {
            Console.WriteLine("TaskVoid {0} {1}", a, b);
        }

        public Task TaskVoidAsync(int a, int b)
        {
            Console.WriteLine("TaskVoidAsync {0} {1}", a, b);
            return Task.CompletedTask;
        }

        public async Task<float> TaskIntAsync(int a, int b)
        {
            Console.WriteLine("SumarAsync {0} {1}", a, b);
            await Task.Delay(500);
            Console.WriteLine("SumarAsync {0} {1} ... ", a, b);
            return (float)(a + b + 1.2323);
        }

        public string FuncDemo(int a, string b = "pepe", CustObj obj = default)
        {
            return $"FuncDemo: a:{a} - b:{b} {JsonSerializer.Serialize(obj)}";
        }

        public string FuncDemo2(int a, string b = "pepe", CustObj obj = default)
        {
            return $"FuncDemo 2 ... A:{a} - B:{b} Obj:{JsonSerializer.Serialize(obj)}";
        }

        public CustObj FuncCustObj(int a, string b = "pepe", CustObj obj = default)
        {
            return new CustObj()
            {
                a = obj?.a ?? a,
                b = new string[] { b, "obj_a: " + obj?.a },
                c = new object[] { a, b }
            };
        }
    }

}
