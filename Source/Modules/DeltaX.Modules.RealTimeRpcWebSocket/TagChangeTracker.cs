namespace DeltaX.Modules.RealTimeRpcWebSocket
{
    using DeltaX.RealTime.Interfaces;
    using DeltaX.RealTime.RtExpression;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class TagChangeTracker
    {
        static List<TagChangeTracker> cacheTags = new List<TagChangeTracker>();

        public static TagChangeTracker GetOrAdd(IRtConnector connector, string tagExpression)
        {
            lock (cacheTags)
            {
                var t = cacheTags.FirstOrDefault(t => t.TagName == tagExpression);
                if (t == null)
                {
                    var tag = (RtTagExpression)RtTagExpression.AddExpression(connector, tagExpression);
                    t = new TagChangeTracker(tag, tagExpression);
                    cacheTags.Add(t);
                }
                return t;
            }
        }

        public static TagChangeTracker[] GetTagsChanged()
        {
            return cacheTags.Where(t => t.IsChanged()).ToArray();
        }

        private TagChangeTracker(RtTagExpression tag, string tagName)
        {
            this.TagName = tagName;
            this.tag = tag;
            this.IsChanged();
        }

        private RtTagExpression tag;
        public string TagName { get; private set; }
        public DateTime Updated { get; set; }
        public DateTime PrevUpdated { get; set; }
        public IRtValue Value { get; set; }
        public IRtValue PrevValue { get; set; }
        public bool Status { get; set; }
        public bool PrevStatus { get; set; }
        public object ValueObject => double.IsNaN(Value.Numeric) || double.IsInfinity(Value.Numeric)
            ? Value.Text
            : Value.Numeric;

        public bool IsChanged()
        {
            PrevStatus = Status;
            PrevUpdated = Updated;
            PrevValue = Value;
            Status = tag.Status;
            Updated = tag.Updated;
            Value = tag.Value;

            return PrevStatus != Status || PrevUpdated != Updated || PrevValue?.Text != Value.Text;
        }
    }
}
