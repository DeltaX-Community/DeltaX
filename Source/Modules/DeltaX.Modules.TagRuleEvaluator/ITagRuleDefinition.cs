namespace DeltaX.Modules.TagRuleEvaluator
{
    using DeltaX.RealTime.RtExpression;
    using System;

    public interface ITagRuleDefinition<TEvent>
    {
        TagRuleCheckType RuleCheckType { get; }
        RtTagExpression TagComparation { get; }
        RtTagExpression TagExpression { get; } 
        int? DiscardValue { get; }
        TEvent EventId { get; }
        DateTime Updated { get; }
        DateTime PrevUpdated { get; }
        double Value { get; }
        double PrevValue { get; }
        double CompareValue { get; }
        void EvaluateChange();
    }
}