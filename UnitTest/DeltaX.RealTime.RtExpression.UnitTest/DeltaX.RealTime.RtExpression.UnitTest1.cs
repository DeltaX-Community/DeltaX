using DeltaX.CommonExtensions;
using DeltaX.MemoryMappedRecord;
using DeltaX.RealTime.Interfaces;
using DeltaX.RealTime.RtMemoryMapped;
using NUnit.Framework;
using System;

namespace DeltaX.RealTime.RtExpression.UnitTest
{
    public class Tests
    {

        IRtConnector conn;
        KeyValueMemoryConfiguration configuration = new KeyValueMemoryConfiguration
        {
            MemoryName = "DemoMemory",
            IndexCapacity = 1_000_000,
            DataCapacity = 1_000_000,
            Persistent = false
        };

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {

            conn = RtConnectorMemoryMapped.Build(configuration, true);

            var tag1 = RtTagExpression.AddExpression(conn, "{tag1}");

            Assert.IsFalse(tag1.Status);
            conn.SetNumeric("tag1", 0);
            Assert.IsTrue(tag1.Status);

            var tagNow = conn.GetOrAddTag("now");
            tagNow.SetDateTime(DateTime.Now);

            DateTimeOffset now = DateTime.Now;
            var tagDateTime = conn.GetOrAddTag("tagDateTime");
            Assert.IsFalse(tagDateTime.Status);
            tagDateTime.SetDateTime(now);
            Assert.IsTrue(tagDateTime.Status);

            var year = conn.AddTagDefinition("tagDateTime@DT:yyyy");
            Assert.AreEqual("2021", year.Value.Text);

            var unixTimestamp = conn.AddTagDefinition("tagDateTime@DT:UNIXTIMESTAMP");
            DateTimeOffset dtparsed = unixTimestamp.Value.Numeric.FromUnixTimestamp();
            Assert.AreEqual(now.ToString("yyyy/MM/dd HH:mm:ss"), dtparsed.ToString("yyyy/MM/dd HH:mm:ss"));
            Assert.AreEqual(now.LocalDateTime.ToUnixTimestamp().ToString("F6"), unixTimestamp.Value.Numeric.ToString("F6"));


            var eUnixTimeSeconds = now.ToUnixTimeSeconds();
            var tagUnixTimeSeconds = conn.AddTagDefinition("tagDateTime@DT:UnixTimeSeconds");
            Assert.AreEqual(eUnixTimeSeconds, (long)tagUnixTimeSeconds.Value.Numeric);

            var eUnixTimeMilliSeconds = now.ToUnixTimeMilliseconds();
            var tagUnixTimeMilliSeconds = conn.AddTagDefinition("tagDateTime@DT:UnixTimeMilliSeconds");
            Assert.AreEqual(eUnixTimeMilliSeconds, (long)tagUnixTimeMilliSeconds.Value.Numeric);


            tagNow.SetDateTime(DateTime.Now.AddMilliseconds(-2));
            tagDateTime.SetDateTime(DateTime.Now.AddMilliseconds(-1));
            var comparator = RtTagExpression.AddExpression(conn, "{now@DT:UnixTimestamp} > {tagDateTime@DT:UnixTimestamp}");
            Assert.AreEqual(0, comparator.Value.Numeric);
            tagNow.SetDateTime(DateTime.Now);
            Assert.AreEqual(1, comparator.Value.Numeric);
            tagDateTime.SetDateTime(DateTime.Now.AddMilliseconds(2));
            Assert.AreEqual(0, comparator.Value.Numeric);

        }
    }
}