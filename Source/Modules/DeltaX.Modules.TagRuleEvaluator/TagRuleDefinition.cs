namespace DeltaX.Modules.TagRuleEvaluator
{
    using DeltaX.RealTime.Interfaces;
    using DeltaX.RealTime.RtExpression;
    using Microsoft.Extensions.Logging;
    using System;

    internal class TagRuleDefinition<TEvent> : ITagRuleDefinition<TEvent>
    {
        private readonly double tolerance;
        private readonly ILogger logger;
        private readonly Func<ITagRuleDefinition<TEvent>, bool> action;

        internal TagRuleDefinition(
            TEvent eventId,
            TagRuleCheckType ruleCheckType,
            IRtTag tagExpression, 
            double tolerance = 0.01,
            double discardValue = 0,
            Func<ITagRuleDefinition<TEvent>, bool> action = null,
            ILogger logger = null)
        {  
            this.logger = logger ?? Configuration.Configuration.DefaultLogger;
            EventId = eventId;
            TagExpression = tagExpression; 
            RuleCheckType = ruleCheckType; 
            this.tolerance = tolerance;
            this.DiscardValue = discardValue;
            this.action = action; 
        }

        public TEvent EventId { get; }
        public IRtTag TagExpression { get; } 
        public TagRuleCheckType RuleCheckType { get; }
        public double DiscardValue { get; private set; }
        public DateTime PrevUpdated { get; private set; }
        public DateTime Updated { get; private set; }
        public double PrevValue { get; private set; }
        public double Value { get; private set; }
        public bool Status { get; private set; }
        public bool PrevStatus { get; private set; }

        private bool HasUpdatedChanged()
        {
            PrevValue = Value;
            PrevUpdated = Updated;
            PrevStatus = Status;
            Value = TagExpression.Value.Numeric;
            Updated = TagExpression.Updated;
            Status = TagExpression.Status;

            return Status 
                && Updated > PrevUpdated 
                && Math.Abs(DiscardValue - Value) > tolerance;
        }

        private bool HasValueChanged()
        {
            PrevValue = Value;
            PrevUpdated = Updated;
            PrevStatus = Status; 
            Value = TagExpression.Value.Numeric;
            Updated = TagExpression.Updated;            
            Status = TagExpression.Status;

            return Status && PrevStatus 
                && !double.IsNaN(PrevValue) 
                && !double.IsInfinity(Value) 
                && Math.Abs(DiscardValue - Value) > tolerance
                && Math.Abs(PrevValue - Value) > tolerance;
        }

        public void EvaluateChange()
        { 
            bool change = false;

            switch (RuleCheckType)
            {
                case TagRuleCheckType.ChangeValue:
                    change = HasValueChanged(); 
                    break; 

                case TagRuleCheckType.ChangeUpdated:
                    change = HasUpdatedChanged(); 
                    break;
            }

            // logger?.LogDebug("Event: [{0}] {1} TagName:[{2}] Expresion:[{3}] PrevValue:{4} Value:{5} Updated:{6} change:{7}",
            //            EventId, RuleCheckType, TagExpression.TagName, TagExpression, PrevValue, Value, Updated, change);

            if (change)
            {
                try
                {
                    logger?.LogDebug("Event: [{0}] {1} TagName:[{2}] Expresion:[{3}] PrevValue:{4} Value:{5} Updated:{6}",
                        EventId, RuleCheckType, TagExpression.TagName, TagExpression, PrevValue, Value, Updated);

                    var ret = action.Invoke(this);
                    if (ret)
                    {
                        logger?.LogDebug("Action return: {0}", ret);
                    }
                    else
                    {
                        logger?.LogInformation("Action return: {0}", ret);
                    }
                }
                catch (Exception e)
                {
                    logger?.LogError("Failed on event:[{0}] Error:'{1}'", EventId, e.Message);
                }
            }
        }
    }
}
