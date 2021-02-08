namespace DeltaX.Modules.TagRuleEventHandler
{
    using DeltaX.RealTime.RtExpression;
    using Microsoft.Extensions.Logging;
    using System;

    internal class TagRuleDefinition<TEvent> : ITagRuleDefinition<TEvent>
    {
        private readonly double tolerance;
        private readonly ILogger logger;
        private readonly Func<TagRuleDefinition<TEvent>, bool> action;

        internal TagRuleDefinition(
            TEvent eventId,
            TagRuleCheckType ruleCheckType,
            RtTagExpression tagExpression,
            RtTagExpression tagComparation,
            int? discardValue = null,
            double tolerance = 0.01,
            Func<TagRuleDefinition<TEvent>, bool> action = null)
        {
            EventId = eventId;
            TagExpression = tagExpression;
            TagComparation = tagComparation;
            RuleCheckType = ruleCheckType;
            DiscardValue = discardValue;
            this.tolerance = tolerance;
            this.action = action;
        }

        public TEvent EventId { get; }
        public RtTagExpression TagExpression { get; }
        public RtTagExpression TagComparation { get; }
        public TagRuleCheckType RuleCheckType { get; }
        public int? DiscardValue { get; }
        public DateTime PrevUpdated { get; private set; }
        public DateTime Updated { get; private set; }
        public double PrevValue { get; private set; }
        public double CompareValue { get; private set; }
        public double Value { get; private set; }

        private bool HasUpdatedChanged()
        {
            if (!TagExpression.Status)
            {
                Value = TagExpression.Value.Numeric;
                Updated = TagExpression.Updated;
                return false;
            }

            PrevValue = Value;
            PrevUpdated = Updated;
            Value = TagExpression.Value.Numeric;
            Updated = TagExpression.Updated;
            return Updated > PrevUpdated;
        }

        private bool HasValueChanged()
        {
            if (!TagExpression.Status
                || double.IsNaN(TagExpression.Value.Numeric)
                || double.IsInfinity(TagExpression.Value.Numeric))
            {
                Value = TagExpression.Value.Numeric;
                Updated = TagExpression.Updated;
                return false;
            }

            PrevValue = Value;
            PrevUpdated = Updated;
            Value = TagExpression.Value.Numeric;
            Updated = TagExpression.Updated;
            return (int)Value != DiscardValue && Math.Abs(PrevValue - Value) > tolerance;
        }

        private bool HasCompartationChange()
        {
            if (!TagExpression.Status
                || !TagComparation.Status
                || double.IsNaN(TagExpression.Value.Numeric)
                || double.IsInfinity(TagExpression.Value.Numeric))
            {
                Value = TagExpression.Value.Numeric;
                Updated = TagExpression.Updated;
                return false;
            }

            CompareValue = TagComparation.Value.Numeric;

            PrevValue = Value;
            PrevUpdated = Updated;
            Value = TagExpression.Value.Numeric;
            Updated = TagExpression.Updated;
            return (int)Value != DiscardValue && Math.Abs(CompareValue - Value) > tolerance;
        }


        public void EvaluateChange()
        { 
            bool change = false;

            switch (RuleCheckType)
            {
                case TagRuleCheckType.ChangeValue:
                    change = HasValueChanged();
                    if (change)
                    {
                        logger.LogInformation("Event: [{0}] {1} TagName:[{2}] Expresion:[{3}] PrevValue:{4} Value:{5} Updated:{6}",
                            EventId, RuleCheckType, TagExpression.TagName, TagExpression.GetExpresionValues(), PrevValue, Value, Updated);
                    }
                    break;

                case TagRuleCheckType.ChangeCompare:
                    change = HasCompartationChange();
                    if (change)
                    {
                        logger.LogInformation("Event: [{0}] {1} TagName:[{2}] Expresion:[{3}] PrevValue:{4} CompareValue:{5} Value:{6} Updated:{7}",
                            EventId, RuleCheckType, TagExpression.TagName, TagExpression.GetExpresionValues(), PrevValue, CompareValue, Value, Updated);
                    }
                    break;

                case TagRuleCheckType.ChangeUpdated:
                    change = HasUpdatedChanged();
                    if (change)
                    {
                        logger.LogInformation("Event: [{0}] {1} TagName:[{2}] Expresion:[{3}] PrevValue:{4} Value:{5} Updated:{6}",
                           EventId, RuleCheckType, TagExpression.TagName, TagExpression.GetExpresionValues(), PrevValue, Value, Updated);
                    }
                    break;
            }

            if (change)
            {
                try
                {
                    bool ret = action.Invoke(this);
                    logger.LogInformation("Action return: {0}", ret);
                }
                catch (Exception e)
                {
                    logger.LogError("Failed on event:[{0}] Error:'{1}'", EventId, e.Message);
                }
            }
        }
    }
}
