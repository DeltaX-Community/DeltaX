namespace DeltaX.Modules.TagRuleEvaluator.UnitTest
{
    using DeltaX.MemoryMappedRecord;
    using DeltaX.RealTime;
    using DeltaX.RealTime.Interfaces;
    using DeltaX.RealTime.RtExpression;
    using DeltaX.RealTime.RtMemoryMapped;
    using NUnit.Framework;
    using System;

    public class TagRuleEvaluatorUnitTest
    {

        enum EventType
        {
            None,
            Event1,
            Event2,
            Event3            
        }

        TagRuleChangeEvaluator<EventType> tagRuleChangeEvaluator;
        IRtConnector conn;
        KeyValueMemoryConfiguration configuration = new KeyValueMemoryConfiguration
        {
            MemoryName = "DemoMemory",
            IndexCapacity = 1_000_000,
            DataCapacity = 1_000_000,
            Persistent = false
        };
               

        [Test]
        public void Test1()
        {
            conn = RtConnectorMemoryMapped.Build(configuration, true);

            EventType eventReceive = EventType.None;
            int eventsCount = 0;

            bool defaultAction(ITagRuleDefinition<EventType> arg)
            {
                eventReceive = arg.EventId;
                eventsCount++;
                var prevValue = arg.PrevValue;
                var value = arg.Value;
                return true;
            }

            tagRuleChangeEvaluator = new TagRuleChangeEvaluator<EventType>(defaultAction);

            var tag1 = new RtTagExpression(conn, "{tagEvent1}");
            var tag2 = new RtTagExpression(conn, "{tagEvent2}");
            var tag3 = new RtTagExpression(conn, "{tagEvent3}");

            tagRuleChangeEvaluator.AddRule(EventType.Event1, TagRuleCheckType.ChangeValue, tag1, null);
            tagRuleChangeEvaluator.AddRule(EventType.Event2, TagRuleCheckType.ChangeValue, tag2, null);
            tagRuleChangeEvaluator.AddRule(EventType.Event3, TagRuleCheckType.ChangeValue, tag3, null);

            Assert.AreEqual(EventType.None, eventReceive);
             
            conn.SetNumeric("tagEvent1", 0);
            conn.SetNumeric("tagEvent2", 0);
            conn.SetNumeric("tagEvent3", 0);
            tagRuleChangeEvaluator.EvaluateChanges();
            conn.SetNumeric("tagEvent1", 1);
            tagRuleChangeEvaluator.EvaluateChanges(); 
            tagRuleChangeEvaluator.EvaluateChanges();
            Assert.AreEqual(EventType.Event1, eventReceive);

            conn.SetNumeric("tagEvent2", 2);
            tagRuleChangeEvaluator.EvaluateChanges();
            conn.SetNumeric("tagEvent2", 2);
            tagRuleChangeEvaluator.EvaluateChanges();
            Assert.AreEqual(EventType.Event2, eventReceive);
            conn.SetNumeric("tagEvent2", 2);
            tagRuleChangeEvaluator.EvaluateChanges();
            Assert.AreEqual(EventType.Event2, eventReceive); 

            conn.SetNumeric("tagEvent3", 1);
            Assert.AreEqual(EventType.Event2, eventReceive);
            tagRuleChangeEvaluator.EvaluateChanges();
            tagRuleChangeEvaluator.EvaluateChanges();
            Assert.AreEqual(EventType.Event3, eventReceive);

            Assert.AreEqual(3, eventsCount);
        }
    }
}