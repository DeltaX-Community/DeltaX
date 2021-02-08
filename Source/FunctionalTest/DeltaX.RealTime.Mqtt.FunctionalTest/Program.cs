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
        static void Main(string[] args)
        {
            LoggerConfiguration.SetSerilog();
            ILogger logger = LoggerConfiguration.DefaultLoggerFactory.CreateLogger("Main");
            logger.LogInformation("Start DeltaX.RealTime.Mqtt.FunctionalTest");

            var config = new MqttConfiguration("Mqtt", "appsettings.json");
            var mqtt = new MqttClientHelper(config);
            var conn =   RtConnectorMqtt.Build(mqtt);
            var t = conn.ConnectAsync();
            t.Wait();
            Thread.Sleep(500);
            var pepe = conn.GetOrAddTag("pepe",null);

            // t.Wait();

            var pepe2 = conn.AddTag("pepe2", "pepe");
            var pruebaDate = conn.AddTag("pruebaDate");


            var pruebaJson = conn.AddTag("pruebaJson");
            var pruebaJsonStr = conn.AddTag("pruebaJson@JSON:arrayString[0]");
            var pruebaJsonFail = conn.AddTag("pruebaJson@JSON:Fail[0]");
            var pruebaJsonObj = conn.AddTag("pruebaJson@JSON:obj.programName");
            var pruebaJsonCounter = conn.AddTag("pruebaJson@JSON:arrayInteger[1]");

            var pruebaCount2 = conn.AddTag("pruebaCount2");
            var pruebaExpression = new RtTagExpression("arg0 + arg1", new[] { pruebaJsonCounter, pruebaCount2 });

            var pruebaExpression2 = new RtTagExpression(conn, "{pruebaCount2} + 1000");

            {
                using (var pruebaExpression3 = new RtTagExpression(conn, "{pruebaCount2} + 1000"))
                {
                    pruebaExpression3.ValueUpdated += PruebaExpression3_OnUpdatedValue;
                    pruebaExpression.ValueUpdated += PruebaExpression3_OnUpdatedValue;

                    Thread.Sleep(500);
                    pruebaCount2.SetNumeric(45);
                    Thread.Sleep(500);
                    pruebaCount2.SetNumeric(46);
                    Thread.Sleep(500);

                    Console.WriteLine("FIN Bloque pruebaExpression3");
                }


                using (var test = conn.AddTag("pruebaCount"))
                {
                    test.ValueUpdated += PruebaExpression3_OnUpdatedValue;

                    Thread.Sleep(500);
                    test.SetNumeric(45);
                    Thread.Sleep(500);
                    test.SetNumeric(46);
                    Thread.Sleep(500);

                    Console.WriteLine("FIN Bloque test");
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
                /// Console.WriteLine($"pepe \t Name:{pepe.TagName}\t Status:{pepe.Status} Value:{pepe.Value.Text} " +
                ///     $"Updated:{pepe.Updated.ToString("o")}");
                /// Console.WriteLine($"pepe2\t Name:{pepe2.TagName}\t Status:{pepe2.Status} Value:{pepe2.Value.Text} " +
                ///     $"Updated:{pepe2.Updated.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture)}");
                /// 
                /// Console.WriteLine($"pruebaDate\t Name:{pruebaDate.TagName}\t Status:{pruebaDate.Status} Value:{pruebaDate.Value.Text} " +
                ///     $"ValueDate:{pruebaDate.GetDateTime(DateTime.MinValue)} ");


                Console.WriteLine($"-- pruebaJson        Status:{pruebaJson.Status} {pruebaJson.TagName} Value:{pruebaJson.Value.Text} ");
                Console.WriteLine($"-- pruebaJsonStr     Status:{pruebaJsonStr.Status} {pruebaJsonStr.TagName} Value:{pruebaJsonStr.Value.Text} ");
                Console.WriteLine($"-- pruebaJsonFail    Status:{pruebaJsonFail.Status} {pruebaJsonFail.TagName} Value:{pruebaJsonFail.Value.Text} ");
                Console.WriteLine($"-- pruebaJsonObj     Status:{pruebaJsonObj.Status} {pruebaJsonObj.TagName} Value:{pruebaJsonObj.Value.Text} ");
                Console.WriteLine($"-- pruebaJsonCounter Status:{pruebaJsonCounter.Status} {pruebaJsonCounter.TagName} Value:{pruebaJsonCounter.Value.Text} ");


                Console.WriteLine($"EXPRESSION: pruebaExpression Status:{pruebaExpression.Status} ExpressionString:{pruebaExpression.GetExpresionValues()} Value:{pruebaExpression.Value.Text}");
                Console.WriteLine($"EXPRESSION: pruebaExpression2 Status:{pruebaExpression2.Status} Value:{pruebaExpression2.Value.Text}");


                Console.WriteLine($"ULTRALIGHT pruebaUL1 Status:{pruebaUL1.Status} {pruebaUL1.TagName} Value:{pruebaUL1.Value.Text} ");
                Console.WriteLine($"ULTRALIGHT pruebaUL1_c1f1 Status:{pruebaUL1_c1f1.Status} Tag:{pruebaUL1.TagName} Value:{pruebaUL1_c1f1.Value.Text} ");


                Thread.Sleep(1000);

                /// // pepe.SetText($"Hola mundo {DateTime.Now}");
                /// conn.WriteValue("pepe", RtValue.Create($"Hola mundo {DateTime.Now}"));
                /// 
                /// // conn.SetText("pruebaDate", DateTime.UtcNow.ToString("o"));
                /// pruebaDate.SetDateTime(DateTime.UtcNow);


                string ul1 = $"device2@cmd{counter}|counter={counter}|counter10={counter * 10}";
                pruebaUL1.SetText(ul1);


                pruebaCount2.SetNumeric(counter * 1.23);
                pruebaJson.SetJson(new
                {
                    programName = "RtHistoricDB",
                    date = DateTime.Now,
                    counter = counter++,
                    arrayString = new string[] { $"hola mundo {counter}", $"el contador es {counter / 100} k", "fake" },
                    arrayInteger = new int[] { counter * 10, counter, counter * 30 },
                    arrayDouble = new double[] { counter * 1e-2, counter * 1e-6, counter * 1e-9 },
                    arrayObject = new object[] {
                            "RtHistoricDB",
                            counter,
                            new int[] { counter, counter*100, counter * 10000}
                        },
                    obj = new
                    {
                        programName = "RtHistoricDB",
                        date = DateTime.Now
                    }
                });

                Thread.Sleep(2000);

            }
        }

        private static void Program_EventTest2(object sender, bool e)
        {
            Console.WriteLine($"Program_EventTest2");
        }

        private static void Program_EventTest1(object sender, bool e)
        {
            Console.WriteLine($"Program_EventTest1");
        }

        private static void Program_EventTest(object sender, bool e)
        {
            Console.WriteLine($"Program_EventTest");
        }

        private static void PruebaExpression3_OnUpdatedValue(object sender, IRtTag e)
        {
            Console.WriteLine($"+++++ PruebaExpression3_OnUpdatedValue TagName:{e.Topic} Value:{e.Value.Text} ");
        }

        private static void Conn_OnSetValue(object sender, IRtTag e)
        {
            Console.WriteLine($"Conn_OnSetValue TagName:{e.TagName} ");
        }

        private static void Conn_OnUpdatedValue(object sender, IRtTag e)
        {
            Console.WriteLine($"Conn_OnUpdatedValue TagName:{e.TagName} ");
        }

        private static void Conn_OnDisconnect(object sender, bool e)
        {
            Console.WriteLine($"Conn_OnDisconnect status:{e} ");
        }

        private static void Conn_OnConnect(object sender, bool e)
        {
            Console.WriteLine($"Conn_OnConnect status:{e} ");
        }

        private static void Conn_OnMessageReceive(object sender, IRtMessage e)
        {
            Console.WriteLine($"Conn_OnMessageReceive Name:{e.Topic} Value:{e.Value.Text} ");
        }
    }
}

