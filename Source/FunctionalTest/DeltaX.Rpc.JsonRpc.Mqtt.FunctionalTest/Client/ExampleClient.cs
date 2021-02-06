using DeltaX.Configuration.Serilog;
using DeltaX.Connections.MqttClientHelper;
using DeltaX.Rpc.JsonRpc.Mqtt.FunctionalTest.Shared;
using DeltaX.Rpc.JsonRpc.MqttConnection;
using Microsoft.Extensions.Logging;
using System; 
using System.Threading.Tasks;

namespace DeltaX.Rpc.JsonRpc.Mqtt.FunctionalTest.Client
{
    public class ExampleClient : INotifications
    {
        private ILogger logger;

        public ExampleClient()
        {
            logger = LoggerConfiguration.DefaultLoggerFactory.CreateLogger("Client");
        }

        public void OnNotifyBroadcast(object message)
        {
            logger.LogInformation("NotificationBroadcast receive message:{0}", message);
        }

        public void OnNotifySum(int result)
        {
            logger.LogInformation("NotifySumar receive result:{0}", result);
        }

        public async Task RunAsync()
        {
            var config = new MqttConfiguration();
            config.ClientId = "Test JsonRpcMqtt" + Guid.NewGuid();
            config.Username = "sima";
            config.Password = "sima";

            var mqtt = new MqttClientHelper(config);

            // Rpc Ojbect
            var rpc = new Rpc(new JsonRpcMqttConnection(mqtt.Client, "test/", clientId: "ExampleClient"));

            // RPC Register methods for notifications
            rpc.Dispatcher.RegisterMethod(this, nameof(OnNotifyBroadcast));
            rpc.Dispatcher.RegisterMethod(this, nameof(OnNotifySum));

            // RPC: UpdateRegisteredMethods for subscribe and receive notifications
            mqtt.OnConnectionChange += (s, connected) =>
            {
                if (connected)
                {
                    rpc.UpdateRegisteredMethods();
                }
            };

            // Map interface with rpc for call services methods.
            var service = rpc.GetServices<IExampleService>();

            // Wait for connect
            mqtt.ConnectAsync().Wait();

            while (mqtt.IsRunning)
            {
                // Call async without service
                var taskBroadcast = rpc.CallAsync("SendBroadcastAsync", $"Mensaje_1 enviado desde el cliente {rpc.Connection.ClientId} " +
                    $"a todos lo clientes, Date:{DateTime.Now}");

                // Call with service
                var taskBroadcast2 = service.SendBroadcastAsync($"Mensaje_2 enviado desde el cliente {rpc.Connection.ClientId} " +
                    $"a todos lo clientes, Date:{DateTime.Now}");

                logger.LogInformation("SendBroadcastAsync task 1:{0} task 2:{1}", taskBroadcast.Status, taskBroadcast2.Status);

                var a = 1;
                var b = DateTime.Now.Second;
                var res = service.Sum(a, b);
                logger.LogInformation("Sum result:{0} expected:{1}", res, (a + b));

                var t = service.TaskVoidAsync(1, DateTime.Now.Second);
                logger.LogInformation("TaskVoidAsync t.Status:{0}", t?.Status);
                t.Wait();
                logger.LogInformation("TaskVoidAsync affter wait t.Status:{0}", t?.Status);

                var t2 = service.TaskFloatAsync(a, b);
                t2.Wait();
                logger.LogInformation("TaskFloatAsync result:{0} expected:{1} status:{2}", t2.Result, a + b + 1.2323, t2.Status);

                string rCall1 = service.Concat("ASDF", a, b);
                logger.LogInformation("Concat result:<{0}> expected:<{1}>", rCall1, $"Formated format:ASDF a:{a} b:{b}");

                CustObj resObj1 = service.FuncCustObj(1234, "hola FuncCustObj");
                logger.LogInformation("GetCaller FuncCustObj Result: {@0}", resObj1);

                resObj1 = service.FuncCustObj(1234, "hola FuncCustObj", new CustObj { a = 23, b = new string[] { "sadf" } });
                logger.LogInformation("GetCaller FuncCustObj Result: {@0} {1}", resObj1, resObj1.c[0]);

                await Task.Delay(5000);
            }
        }
    }
}
