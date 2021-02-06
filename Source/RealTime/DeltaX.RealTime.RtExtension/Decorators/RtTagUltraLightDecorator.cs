namespace DeltaX.RealTime.Decorators
{
    using DeltaX.CommonExtensions; 
    using DeltaX.RealTime.Interfaces;
    using System;
    using System.Text.Json;

    public class RtTagUltraLightDecorator : RtTagDecoratorBase
    {
        private DateTime parsedTime;
        private string ulField;
        private string ulCommand;
        private string ulDevice;
        private IRtValue currentValueParsed;

        public RtTagUltraLightDecorator(IRtTag tag, string ultraLightValuePattern) : base(tag)
        {
            if(string.IsNullOrEmpty(ultraLightValuePattern))
            {
                throw new ArgumentNullException(nameof(ultraLightValuePattern));
            }

            if(!ultraLightValuePattern.TryUltraLightParse(out ulDevice, out ulCommand, out ulField))
            {
                throw new ArgumentException("Bad UltraLight expression", nameof(ultraLightValuePattern));
            }

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

        private IRtValue TryParseValue(string value, DateTime updated)
        {
            lock (this)
            {
                try
                {                    
                    var parsed = value.UltraLightGetValue(ulField, ulCommand, ulDevice);                      
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
            throw new InvalidOperationException($"Tag {TagName} is type UltraLight, Write is not supported!");
        }
    }
}
