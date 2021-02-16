namespace DeltaX.RealTime
{
    using DeltaX.RealTime.Interfaces;
    using DeltaX.RealTime;
    using System;
    using DeltaX.RealTime.Decorators;
    using System.Text.Json;
     
    public static class RtConnectorExtensions
    {
        public static bool SetNumeric(this IRtConnector conn, string topic, double value, IRtTagOptions options = null)
        {
            return conn.WriteValue(topic, RtValue.Create(value), options);
        }

        public static bool SetText(this IRtConnector conn, string topic, string value, IRtTagOptions options = null)
        {
            return conn.WriteValue(topic, RtValue.Create(value), options);
        }

        public static bool SetBinary(this IRtConnector conn, string topic, byte[] value, IRtTagOptions options = null)
        {
            return conn.WriteValue(topic, RtValue.Create(value), options);
        }

        public static bool SetJson<T>(this IRtConnector conn, string topic, T json, IRtTagOptions options = null)
        {
            return conn.WriteValue(topic, RtValue.Create(JsonSerializer.Serialize(json)), options);
        }


        public static IRtTag AddTag(this IRtConnector conn, string topic, IRtTagOptions options = null)
        {
            return conn.AddTag(topic, topic, options);
        }

        public static IRtTag GetOrAddTag(this IRtConnector conn, string topic, IRtTagOptions options = null)
        {
            return conn.GetOrAddTag(topic, topic, options);
        }

        public static IRtTag GetOrAddTag(this IRtConnector conn, string tagName, string topic, IRtTagOptions options = null)
        {
            var tag = conn.GetTag(tagName);
            if (tag == null)
            {
                tag = conn.AddTag(tagName, topic, options);
            }
            return tag;
        }

        public static IRtTag AddTagDefinition(this IRtConnector conn, string tagDefinition, IRtTagOptions options = null)
        {
            var tsplit = tagDefinition.Split(new[] { "@" }, 2, StringSplitOptions.None);
            var topic = tsplit[0];

            if (tsplit.Length > 1)
            {
                var tag = conn.GetOrAddTag(topic, options);

                var tagDefinitionPattern = tsplit[1];

                if (tagDefinitionPattern.StartsWith("JSON:"))
                {
                    var jsonValuePattern = tagDefinitionPattern.Remove(0, 5);
                    return new RtTagJson(tag, jsonValuePattern);
                }
                else if (tagDefinitionPattern.StartsWith("RGX:"))
                {
                    var regexPattern = tagDefinitionPattern.Remove(0, 4);
                    return new RtTagRegex(tag, regexPattern);
                }  
                else if (tagDefinitionPattern.StartsWith("DT:"))
                {
                    var dateTimePattern = tagDefinitionPattern.Remove(0, 3);
                    return new RtTagDateTime(tag, dateTimePattern);
                }
                else if (tagDefinitionPattern.StartsWith("UL:"))
                {
                    var ultraLightValuePattern = tagDefinitionPattern.Remove(0, 3);
                    return new RtTagUltraLight(tag, ultraLightValuePattern);
                }
            }

            return conn.GetOrAddTag(topic, options);
        }

    }
}

