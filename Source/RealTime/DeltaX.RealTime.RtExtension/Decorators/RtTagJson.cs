namespace DeltaX.RealTime.Decorators
{
    using DeltaX.CommonExtensions; 
    using DeltaX.RealTime.Interfaces;
    using System;
    using System.Text.Json;

    public class RtTagJson : RtTagDecoratorBase, IRtTag
    {
        private DateTime parsedTime;
        private IRtValue currentValueParsed;

        public RtTagJson(IRtTag tag, string jsonValuePattern) : base(tag)
        {
            TagJsonValuePattern = jsonValuePattern;
            currentValueParsed = TryParseValue(tag.Value.Text);
            parsedTime = tag.Updated;
        }
         
        public string TagJsonValuePattern { get; protected set; }
       
        public override IRtValue Value
        {
            get
            {
                if (parsedTime != Updated)
                {
                    currentValueParsed = TryParseValue(tag.Value.Text);
                    parsedTime = tag.Updated;
                }
                return currentValueParsed;
            }
        }

        private IRtValue TryParseValue(string json)
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
            }
        }

        public override bool Set(IRtValue value)
        {
            throw new InvalidOperationException($"Tag {TagName} is type JSON field, Write is not supported!");
        }
    }
}
