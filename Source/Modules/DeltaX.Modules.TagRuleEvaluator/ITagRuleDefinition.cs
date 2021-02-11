namespace DeltaX.Modules.TagRuleEvaluator
{
    using DeltaX.RealTime.Interfaces; 
    using System;

    public interface ITagRuleDefinition<TEvent>
    {
        TagRuleCheckType RuleCheckType { get; }
        IRtTag TagComparation { get; }
        IRtTag TagExpression { get; }  
        TEvent EventId { get; }
        DateTime Updated { get; }
        DateTime PrevUpdated { get; }
        double Value { get; }
        double PrevValue { get; }
        double CompareValue { get; }
        void EvaluateChange();
    }
}