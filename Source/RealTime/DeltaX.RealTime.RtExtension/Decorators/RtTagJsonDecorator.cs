namespace DeltaX.RealTime.Decorators
{
    using DeltaX.CommonExtensions; 
    using DeltaX.RealTime.Interfaces;
    using System;
    using System.Text.Json;

    public class RtTagJsonDecorator : RtTagDecoratorBase
    {
        private DateTime parsedTime;
        private IRtValue currentValueParsed;

        public RtTagJsonDecorator(IRtTag tag, string jsonValuePattern) : base(tag)
        {
            TagJsonValuePattern = jsonValuePattern;
            currentValueParsed = TryParseValue(tag.Value.Text, tag.Updated);
        }
         
        public string TagJsonValuePattern { get; protected set; }
       
        public override IRtValue Value
        {
            get
            {
                if (parsedTime != Updated)
                {
                    currentValueParsed = TryParseValue(tag.Value.Text, tag.Updated);
                }
                return currentValueParsed;
            }
        }

        private IRtValue TryParseValue(string json, DateTime updated)
        {
            lock (this)
            {
                try
                {
                    var jsonObject = JsonSerializer.Deserialize<JsonElement>(json);
                    var obj = jsonObject.JsonGetValue(TagJsonValuePattern);
                    var parsed = Convert.ToString(obj);
                    return RtValue.Create(parsed);
                }
                catch
                {
                    return RtValue.Create(string.Empty);
                }
                finally
                {
                    parsedTime = updated;
                }
            }
        }

        public override bool Set(IRtValue value)
        {
            throw new InvalidOperationException($"Tag {TagName} is type JSON field, Write is not supported!");
        }
    }
}
