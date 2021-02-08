using DeltaX.Configuration.Serilog;
using DeltaX.Connections.MqttClientHelper;
using DeltaX.Rpc.JsonRpc.Mqtt.FunctionalTest.Client;
using DeltaX.Rpc.JsonRpc.Mqtt.FunctionalTest.Server;
using DeltaX.Rpc.JsonRpc.MqttConnection;
using System;
using System.Threading.Tasks;

namespace DeltaX.Rpc.JsonRpc.Mqtt.FunctionalTest
{
    class Program
    {

        static Task RunRpcServer()
        {
            var config = new MqttConfiguration();
            config.ClientId = "Test JsonRpcMqtt" + Guid.NewGuid();
            config.Username = "sima";
            config.Password = "sima";

            var mqtt = new MqttClientHelper(config);
            var rpc = new Rpc(new JsonRpcMqttConnection(mqtt.Client, "test/", clientId: "ExampleService"));

            var service = new ExampleService(rpc);
            rpc.Dispatcher.RegisterService(service);

            mqtt.OnConnectionChange += (s, connected) =>
            {
                if (connected)
                {
                    rpc.UpdateRegisteredMethods();
                }
            };

            return mqtt.RunAsync();
        }


        static void Main(string[] args)
        {
            Console.WriteLine("DeltaX.Rpc.JsonRpc.Mqtt.FunctionalTest Started!"); 

            LoggerConfiguration.SetSerilog();

            var server = RunRpcServer();
            var client = new ExampleClient(); 
            Task.WaitAll(server, client.RunAsync());


            DateTime now = DateTime.Now;

            now.ToOADate();
        }
    }
}
