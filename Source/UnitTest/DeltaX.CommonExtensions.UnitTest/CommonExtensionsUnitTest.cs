using NUnit.Framework;
using System;
using System.Text.Json;

namespace DeltaX.CommonExtensions.UnitTest
{
    public class CommonExtensionsUnitTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [Theory]
        [TestCase(1)] 
        [TestCase(-10)] 
        public void DeserializeUltraLight(int counter)
        {
            string cmd1 = $"device1@comando1|{counter}";
            var c1_r1 = cmd1.UltraLightGetValue("value", "comando1");
            var c1_r2 = cmd1.UltraLightGetValue("value", "comando1", "device1");
            Assert.AreEqual(counter.ToString(), c1_r1);
            Assert.AreEqual(counter.ToString(), c1_r2);


            string cmd2 = $"device2@comando2|counter={counter}|counter10={counter * 10}";
            var c2_r10 = cmd2.UltraLightGetValue("counter10", "comando2");
            var c2_r1 = cmd2.UltraLightGetValue("counter", "comando2", "device2");
            Assert.AreEqual((counter * 10).ToString(), c2_r10);
            Assert.AreEqual(counter.ToString(), c1_r1);


            string msg1 = $"counter|{counter}|counter10|{counter * 10}";
            var m1_r10 = msg1.UltraLightGetValue("counter10");
            var m1_r1 = msg1.UltraLightGetValue("counter");
            Assert.AreEqual((counter * 10).ToString(), m1_r10);
            Assert.AreEqual(counter.ToString(), m1_r1);


            string msg2 = $"{DateTime.Now.ToString("o")}|counter2|{counter * 2}|counter20|{counter * 20}";
            var m2_r20 = msg2.UltraLightGetValue("counter20");
            var m2_fail = msg2.UltraLightGetValue("counter_fail");
            var m2_r2 = msg2.UltraLightGetValue("counter2");
            Assert.AreEqual((counter * 20).ToString(), m2_r20);
            Assert.AreEqual((counter * 2).ToString(), m2_r2);
            Assert.Null(m2_fail);
        }


        [Theory]  
        [TestCase("device1@command1|field1")]
        [TestCase("command1|field1")]
        [TestCase("field1")]
        public void ParseUltraLight(string patternParser)
        {
            string field;
            string command;
            string device;

            var parsed = patternParser.TryUltraLightParse(out device, out command, out field);

            Assert.True(parsed);

            string res = "";

            if (!string.IsNullOrEmpty(device))
                res += $"{device}@";

            if (!string.IsNullOrEmpty(command))
                res += $"{command}|";

            if (!string.IsNullOrEmpty(field))
                res += $"{field}";


            Assert.AreEqual(patternParser, res);  
        }

        [Theory]
        [TestCase(1)]
        [TestCase(-10)]
        public void SerializeJson(int counter)
        {
            var json = new
            {
                programName = "RtHistoricDB",
                date = DateTime.Now,
                counter = counter,
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
            };

            var jsonStr = JsonSerializer.Serialize(json);
            JsonElement result = JsonSerializer.Deserialize<JsonElement>(jsonStr);

            Assert.AreEqual((double)counter, result.JsonGetValue("counter")); 
            Assert.AreEqual($"hola mundo {counter}", result.JsonGetValue("arrayString[0]"));
            Assert.AreEqual($"fake", result.JsonGetValue("arrayString[2]")); 

            Assert.Throws<InvalidOperationException>(() => result.JsonGetValue("arrayString_narrayStringotExist[101]")); 
            Assert.Throws<InvalidOperationException>(() => result.JsonGetValue("arrayString_notExist[0]")); 
            Assert.Throws<InvalidOperationException>(() => result.JsonGetValue("arrayString_notExist")); 

            Assert.AreEqual($"{counter}", Convert.ToString(result.JsonGetValue("counter"))); 
            Assert.Throws<InvalidOperationException>(() => Convert.ToString(result.JsonGetValue("counter_not_exist")));

            var arrayObject = result.JsonGetValue("arrayObject");
            Assert.NotNull(arrayObject);

            string str = JsonSerializer.Serialize(json.arrayObject);
            Assert.AreEqual(str, Convert.ToString(arrayObject));


            Assert.AreEqual(json.obj.programName, result.JsonGetValue("obj.programName"));
        }
    }
}