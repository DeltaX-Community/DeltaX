namespace DeltaX.RealTime.Decorators
{
    using DeltaX.RealTime; 
    using DeltaX.CommonExtensions; 
    using DeltaX.RealTime.Interfaces;
    using System;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    public class RtTagDateTime : RtTagDecoratorBase, IRtTag
    {
        private DateTime parsedTime;
        private IRtValue currentValueParsed = valueNull;
        private bool status = false;

        public RtTagDateTime(IRtTag tag, string dateTimePatter) : base(tag)
        {
            TagDateTimeValuePattern = dateTimePatter;
            currentValueParsed = TryParseValue(tag);
            parsedTime = Updated;
        }

        public string TagDateTimeValuePattern { get; protected set; }

        public override bool Status
        {
            get
            {
                if (parsedTime != Updated)
                {
                    currentValueParsed = TryParseValue(tag);
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
                    currentValueParsed = TryParseValue(tag);
                    parsedTime = Updated;
                }
                return currentValueParsed;
            }
        }

        private IRtValue TryParseValue(IRtTag tag)
        {
            lock (this)
            {
                try
                {
                    DateTimeOffset dt = tag.GetDateTime();
                    var parsed = string.Empty;

                    switch (TagDateTimeValuePattern.ToUpper())
                    {
                        case "UNIXTIMESTAMP":                              
                            parsed = dt.LocalDateTime.ToUnixTimestamp().ToString();
                            break;
                        case "UNIXTIMEMILLISECONDS":
                            parsed = dt.ToUnixTimeMilliseconds().ToString();
                            break;
                        case "UNIXTIMESECONDS":
                            parsed = dt.ToUnixTimeSeconds().ToString();
                            break;
                        default:
                            parsed = dt.ToString(TagDateTimeValuePattern);
                            break;
                    }

                    Status = !string.IsNullOrEmpty(parsed) && base.Status;
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
            throw new InvalidOperationException($"Tag {TagName} is type JSON field, Write is not supported!");
        }
    }
}
