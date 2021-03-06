﻿namespace DeltaX.RealTime.Decorators
{
    using DeltaX.CommonExtensions; 
    using DeltaX.RealTime.Interfaces;
    using System;
    using System.Text.Json;

    public class RtTagUltraLight : RtTagDecoratorBase
    {
        private DateTime parsedTime;
        private string ulField;
        private string ulCommand;
        private string ulDevice;
        private IRtValue currentValueParsed = valueNull;
        private bool status = false;

        public RtTagUltraLight(IRtTag tag, string ultraLightValuePattern) : base(tag)
        {
            if(string.IsNullOrEmpty(ultraLightValuePattern))
            {
                throw new ArgumentNullException(nameof(ultraLightValuePattern));
            }

            if(!ultraLightValuePattern.TryUltraLightParse(out ulDevice, out ulCommand, out ulField))
            {
                throw new ArgumentException("Bad UltraLight expression", nameof(ultraLightValuePattern));
            }

            currentValueParsed = TryParseValue(tag.Value.Text);
            parsedTime = Updated;
        }
         
        public string TagJsonValuePattern { get; protected set; }


        public override bool Status
        {
            get
            {
                if (parsedTime != Updated)
                {
                    currentValueParsed = TryParseValue(tag.Value.Text);
                    parsedTime = Updated;
                }
                return base.Status && status;
            }

            protected set
            {
                if (status != value)
                {
                    status = value;
                    OnStatusChanged(this, this);
                }
            }
        }

        public override IRtValue Value
        {
            get
            {
                if (parsedTime != Updated)
                {
                    currentValueParsed = TryParseValue(tag.Value.Text);
                    parsedTime = Updated;
                }
                return currentValueParsed;
            }
        }

        private IRtValue TryParseValue(string value)
        {
            lock (this)
            {
                try
                {                    
                    var parsed = value.UltraLightGetValue(ulField, ulCommand, ulDevice);
                    Status = true;
                    return RtValue.Create(parsed);
                }
                catch
                {
                    Status = false;
                    return RtValue.Create(string.Empty);
                } 
            }
        }

        public override bool Set(IRtValue value)
        {
            throw new InvalidOperationException($"Tag {TagName} is type UltraLight, Write is not supported!");
        }
    }
}
