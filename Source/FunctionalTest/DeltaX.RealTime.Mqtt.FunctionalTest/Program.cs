using DeltaX.Configuration.Serilog;
using DeltaX.Connections.MqttClientHelper;
using DeltaX.RealTime.Interfaces;
using DeltaX.RealTime.RtExpression;
using DeltaX.RealTime.RtMqtt;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace DeltaX.RealTime.Mqtt.FunctionalTest
{
    class Program
    {
        static ILogger logger;

        static void Main(string[] args)
        {
            LoggerConfiguration.SetSerilog();
            logger = LoggerConfiguration.DefaultLoggerFactory.CreateLogger("Main");
            logger.LogInformation("Start DeltaX.RealTime.Mqtt.FunctionalTest");

            var config = new MqttConfiguration("Mqtt", "appsettings.json");
            var mqtt = new MqttClientHelper(config);
            var conn =   RtConnectorMqtt.Build(mqtt);

            conn.Connected += Conn_Connected;
            conn.ValueUpdated += Conn_ValueUpdated;
            var t = conn.ConnectAsync();
            t.Wait();
            Thread.Sleep(500);
            var pepe = conn.GetOrAddTag("pepe",null);

            // t.Wait();

            var pepe2 = conn.AddTag("pepe2", "pepe");
            var pruebaDate = conn.AddTag("pruebaDate");


            var pruebaJson = conn.AddTag("pruebaJson");
            var pruebaJsonStr = conn.AddTagDefinition("pruebaJson@JSON:arrayString[0]");
            var pruebaJsonFail = conn.AddTagDefinition("pruebaJson@JSON:Fail[0]");
            var pruebaJsonObj = conn.AddTagDefinition("pruebaJson@JSON:obj.programName");
            var pruebaJsonCounter = conn.AddTagDefinition("pruebaJson@JSON:arrayInteger[1]");

            var pruebaCount2 = conn.AddTag("pruebaCount2");
            var pruebaExpression = new RtTagExpression("arg0 + arg1", new[] { pruebaJsonCounter, pruebaCount2 });

            var pruebaExpression2 = new RtTagExpression(conn, "{pruebaCount2} + 1000");

            {
                using (var pruebaExpression3 = new RtTagExpression(conn, "{pruebaCount2} + 1000"))
                {
                    pruebaExpression3.ValueUpdated += OnUpdatedValue;
                    pruebaExpression.ValueUpdated += OnUpdatedValue;

                    Thread.Sleep(500);
                    pruebaCount2.SetNumeric(45);
                    Thread.Sleep(500);
                    pruebaCount2.SetNumeric(46);
                    Thread.Sleep(500);

                    logger.LogInformation("FIN Bloque pruebaExpression3");
                }

                using (var test = conn.AddTag("pruebaCount"))
                {
                    test.ValueUpdated += OnUpdatedValue;

                    Thread.Sleep(500);
                    test.SetNumeric(45);
                    Thread.Sleep(500);
                    test.SetNumeric(46);
                    Thread.Sleep(500);

                    logger.LogInformation("FIN Bloque test");
                }
            }


            var pruebaUL1 = conn.GetOrAddTag("pruebaUL1");
            var asdf = conn.GetOrAddTag("pruebaUL1");
            var pruebaUL1_f1 = conn.AddTag("pruebaUL1@UL:field1");
            var pruebaUL1_c1f1 = conn.AddTag("pruebaUL1@UL:cmd1|counter10");
            var pruebaUL1_d1c1f1 = conn.AddTag("pruebaUL1@UL:device1@cmd1|field1");


            int counter = 0;
            while (true)
            { 
                logger.LogInformation("-- {0} Status:{1} Value:{@2} ", pruebaJson.TagName, pruebaJson.Status, pruebaJson.Value.Text);
                logger.LogInformation("-- {0} Status:{1} Value:{@2} ", pruebaJsonStr.TagName, pruebaJsonStr.Status, pruebaJsonStr.Value.Text);
                logger.LogInformation("-- {0} Status:{1} Value:{@2} ", pruebaJsonFail.TagName, pruebaJsonFail.Status, pruebaJsonFail.Value.Text);
                logger.LogInformation("-- {0} Status:{1} Value:{@2} ", pruebaJsonObj.TagName, pruebaJsonObj.Status, pruebaJsonObj.Value.Text); 
                logger.LogInformation("-- {0} Status:{1} Value:{@2} ", pruebaJsonCounter.TagName, pruebaJsonCounter.Status, pruebaJsonCounter.Value.Numeric);  

                logger.LogInformation("-- EXPRESSION {0} Status:{1} GetExpresionValues:{@2} Value:{@3} ", 
                    pruebaExpression.TagName, pruebaExpression.Status, pruebaExpression.GetExpresionValues(), pruebaExpression.Value.Numeric);
                logger.LogInformation("-- EXPRESSION {0} Status:{1} GetExpresionValues:{@2} Value:{@3} ",
                    pruebaExpression2.TagName, pruebaExpression2.Status, pruebaExpression2.GetExpresionValues(), pruebaExpression2.Value.Numeric);

                logger.LogInformation("-- ULTRALIGHT {0} Status:{1} Value:{@2}", pruebaUL1.TagName, pruebaUL1.Status, pruebaUL1.Value.Numeric);
                logger.LogInformation("-- ULTRALIGHT {0} Status:{1} Value:{@2}", pruebaUL1_c1f1.TagName, pruebaUL1_c1f1.Status, pruebaUL1_c1f1.Value.Numeric);
                 
                Thread.Sleep(1000);
                  
                string ul1 = $"device2@cmd{counter}|counter={counter}|counter10={counter * 10}";
                pruebaUL1.SetText(ul1);

                pruebaCount2.SetNumeric(counter * 1.23);
                pruebaJson.SetJson(new
                {
                    programName = "DeltaX.RealTime.Mqtt.FunctionalTest",
                    date = DateTime.Now,
                    counter = counter++,
                    arrayString = new string[] { $"hola mundo {counter}", $"el contador es {counter / 100} k", "fake" },
                    arrayInteger = new int[] { counter * 10, counter, counter * 30 },
                    arrayDouble = new double[] { counter * 1e-2, counter * 1e-6, counter * 1e-9 },
                    arrayObject = new object[] {
                            "DeltaX.RealTime.Mqtt.FunctionalTest",
                            counter,
                            new int[] { counter, counter*100, counter * 10000}
                        },
                    obj = new
                    {
                        programName = "DeltaX.RealTime.Mqtt.FunctionalTest",
                        date = DateTime.Now
                    }
                });

                Thread.Sleep(2000);

            }
        }

        private static void Conn_ValueUpdated(object sender, IRtTag e)
        {
            logger.LogInformation($"+++++ Connection ValueUpdated TagName:{e.Topic} Value:{e.Value} ");
        }

        private static void Conn_Connected(object sender, bool e)
        {
            logger.LogInformation($"+++++ Connected!");
        }

        private static void OnUpdatedValue(object sender, IRtTag e)
        {
            logger.LogInformation($"+++++ OnUpdatedValue TagName:{e.Topic} Value:{e.Value} ");
        }
         
    }
}

