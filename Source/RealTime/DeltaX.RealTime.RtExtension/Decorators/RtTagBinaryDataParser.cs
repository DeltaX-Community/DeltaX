namespace DeltaX.RealTime.Decorators
{
    using DeltaX.RealTime;
    using DeltaX.CommonExtensions;
    using DeltaX.RealTime.Interfaces;
    using System;

    public class RtTagBinaryDataParser : RtTagDecoratorBase, IRtTag
    {
        private DateTime parsedTime;
        private IRtValue currentValueParsed = valueNull;
        private bool status = false;

        public RtTagBinaryDataParser(IRtTag tag, string dateTimePatter) : base(tag)
        {
            TagBinaryDataParserPattern = dateTimePatter;
            currentValueParsed = TryParseValue(tag);
            parsedTime = Updated;
        }

        public string TagBinaryDataParserPattern { get; protected set; }

        public override string TagName => $"{tag.TagName}@BDP:{TagBinaryDataParserPattern}";

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
                    var parsed = tag.Value.Binary.Parser(TagBinaryDataParserPattern);
                    Status = parsed != null && base.Status;

                    return (parsed is string parserStr)
                        ? RtValue.Create(parserStr)
                        : RtValue.Create(Convert.ToDouble(parsed));
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
            throw new InvalidOperationException($"Tag {TagName}, Write is not supported!");
        }
    }
}
