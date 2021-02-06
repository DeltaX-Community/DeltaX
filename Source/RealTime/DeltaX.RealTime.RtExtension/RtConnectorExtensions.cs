namespace DeltaX.RealTime
{
    using DeltaX.RealTime.Interfaces;
    using DeltaX.RealTime;
    using System;
    using DeltaX.RealTime.Decorators;
    using System.Text.Json;
     
    public static class RtConnectorExtensions
    {
        public static bool SetNumeric(this IRtConnector conn, string topic, double value)
        {
            return conn.WriteValue(topic, RtValue.Create(value));
        }

        public static bool SetText(this IRtConnector conn, string topic, string value)
        {
            return conn.WriteValue(topic, RtValue.Create(value));
        }

        public static bool SetBinary(this IRtConnector conn, string topic, byte[] value)
        {
            return conn.WriteValue(topic, RtValue.Create(value));
        }

        public static bool SetJson<T>(this IRtConnector conn, string topic, T json)
        {
            return conn.WriteValue(topic, RtValue.Create(JsonSerializer.Serialize(json)));
        }

        public static IRtTag GetOrAddTag(this IRtConnector conn, string topic)
        {
            return conn.GetOrAddTag(topic, topic);
        }

        public static IRtTag GetOrAddTag(this IRtConnector conn, string tagName, string topic)
        {
            var tag = conn.GetTag(tagName);
            if (tag == null)
            {
                tag = conn.AddTag(tagName, topic);
            }
            return tag;
        }

        public static IRtTag AddTagDefinition(this IRtConnector conn, string tagDefinition)
        {
            // string tagTypeParser = string.Empty;

            var tsplit = tagDefinition.Split(new[] { "@" }, 2, StringSplitOptions.None);
            var topic = tsplit[0];

            if (tsplit.Length > 1)
            {
                var tag = conn.GetOrAddTag(topic);

                var tagDefinitionPattern = tsplit[1];

                if (tagDefinitionPattern.StartsWith("JSON:"))
                {
                    var jsonValuePattern = tagDefinitionPattern.Remove(0, 5);
                    return new RtTagJsonDecorator(tag, jsonValuePattern);
                }
                else if (tagDefinitionPattern.StartsWith("RGX:"))
                {
                    // var regexPattern = tagDefinitionPattern.Remove(0, 4); 
                }
                else if (tagDefinitionPattern.StartsWith("UL:"))
                {
                    var ultraLightValuePattern = tagDefinitionPattern.Remove(0, 3);
                    return new RtTagUltraLightDecorator(tag, ultraLightValuePattern);
                }
            }

            return conn.GetOrAddTag(topic);
        }

    }
}

