namespace DeltaX.RealTime.Decorators
{
    using DeltaX.CommonExtensions; 
    using DeltaX.RealTime.Interfaces;
    using System;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    public class RtTagRegex : RtTagDecoratorBase, IRtTag
    {
        private DateTime parsedTime;
        private IRtValue currentValueParsed = valueNull;
        private bool status = false;
        private Regex regex;

        public RtTagRegex(IRtTag tag, string regexPattern) : base(tag)
        {
            TagRegexValuePattern = regexPattern; 
            currentValueParsed = TryParseValue(tag.Value.Text);
            parsedTime = Updated;
        }
         
        public string TagRegexValuePattern { get; protected set; }

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
                if(status !=value)
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
                    regex ??= new Regex(TagRegexValuePattern);

                    var match =  regex.Match(value); 
                    if(match.Success)
                    {
                        // TODO FIXME: hay que ver si es el grupo uno por defecto. y de agregar un name el agrupo
                        var parsed = match.Groups[1].Value;
                        Status = base.Status;
                        return RtValue.Create(parsed);
                    }
                    else
                    {
                        Status = false;
                        return RtValue.Create(string.Empty);
                    }
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
            throw new InvalidOperationException($"Tag {TagName} is type JSON field, Write is not supported!");
        }
    }
}
