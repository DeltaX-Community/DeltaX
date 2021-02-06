using DeltaX.Configuration.Serilog;
using DeltaX.Connections.MqttClientHelper;
using DeltaX.Rpc.JsonRpc.Mqtt.FunctionalTest.Shared;
using DeltaX.Rpc.JsonRpc.MqttConnection;
using Microsoft.Extensions.Logging;
using System; 
using System.Threading.Tasks;

namespace DeltaX.Rpc.JsonRpc.Mqtt.FunctionalTest.Client
{
    public class ExampleClient: INotifications
    {
        private ILogger logger;

        public ExampleClient()
        {
            logger = LoggerConfiguration.DefaultLoggerFactory.CreateLogger("Client");
        }

        public void NotificationBroadcast(object message)
        {
            logger.LogInformation("NotificationBroadcast receive message:{0}", message);
        }

        public void NotifySumar(int result)
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

            var rpc = new Rpc(new JsonRpcMqttConnection(mqtt.Client, "test/", clientId: "TestRpcClient"));
            mqtt.OnConnectionChange += (s, connected) =>
            {
                if (connected)
                {
                    rpc.UpdateRegisteredMethods();
                }
            };

            rpc.Dispatcher.RegisterMethod(this, nameof(NotificationBroadcast));
            rpc.Dispatcher.RegisterMethod(this, nameof(NotifySumar));

            var service = rpc.GetServices<IExampleService>();

            mqtt.ConnectAsync().Wait();

            while (mqtt.IsRunning)
            {
                var taskBroadcast = rpc.CallAsync("SendBroadcastAsync", "hola mundo " + DateTime.Now);
                var taskBroadcast2 = service.SendBroadcastAsync("hola mundo " + DateTime.Now);
                logger.LogInformation("SendBroadcastAsync taskBroadcast:{0} {1}", taskBroadcast.Status, taskBroadcast2.Status);

                var res = service.Sumar(1, DateTime.Now.Second);
                logger.LogInformation("Sumar result:{0}", res);

                var t = service.TaskVoidAsync(1, DateTime.Now.Second);
                logger.LogInformation("TaskVoidAsync t.Status:{0}", t?.Status);
                t.Wait();
                logger.LogInformation("TaskVoidAsync t.Wait() t.Status:{0}", t?.Status);

                var t2 = service.TaskIntAsync(1, DateTime.Now.Second);
                logger.LogInformation("TaskIntAsync t2.Result:{0}", t2.Result);

                string rCall1 = service.Concatenar("ASDF", 1, 3);
                logger.LogInformation("GetCaller Concatenar Result: {0}", rCall1);

                CustObj resObj1 = service.FuncCustObj(1234, "hola FuncCustObj");
                logger.LogInformation("GetCaller FuncCustObj Result: {@0}", resObj1);

                resObj1 = service.FuncCustObj(1234, "hola FuncCustObj", new CustObj { a = 23, b = new string[] { "sadf" } });
                logger.LogInformation("GetCaller FuncCustObj Result: {@0} {1}", resObj1, resObj1.c[0]);

                await Task.Delay(5000);
            }
        }
    }
}
