namespace DeltaX.Modules.TagRuleEvaluator
{
    using DeltaX.RealTime.Interfaces; 
    using System;

    public interface ITagRuleDefinition<TEvent>
    {
        TagRuleCheckType RuleCheckType { get; } 
        IRtTag TagExpression { get; }  
        TEvent EventId { get; }
        double DiscardValue { get; }
        DateTime Updated { get; }
        DateTime PrevUpdated { get; }
        double Value { get; }
        double PrevValue { get; }        
        void EvaluateChange();
    }
}