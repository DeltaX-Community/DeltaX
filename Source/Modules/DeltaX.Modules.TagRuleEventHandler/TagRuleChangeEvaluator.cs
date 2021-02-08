namespace DeltaX.Modules.TagRuleEventHandler
{
    using DeltaX.RealTime.Interfaces;
    using DeltaX.RealTime.RtExpression;
    using System;
    using System.Collections.Generic;


    public class TagRuleChangeEvaluator<TEvent>
    {
        private List<ITagRuleDefinition<TEvent>> tagRules;
        private Func<ITagRuleDefinition<TEvent>, bool> defaultAction = null;
        private readonly double defaultTolerance;
        private readonly int? defaultDiscardValue;

        public TagRuleChangeEvaluator(
            Func<ITagRuleDefinition<TEvent>, bool> defaultAction = null,
            double defaultTolerance = 0.01,
            int? defaultDiscardValue = null)
        {
            this.tagRules = new List<ITagRuleDefinition<TEvent>>();
            this.defaultAction = defaultAction;
            this.defaultTolerance = defaultTolerance;
            this.defaultDiscardValue = defaultDiscardValue;
        }

        public void AddRule(
            TEvent eventId,
            TagRuleCheckType ruleCheckType,
            RtTagExpression tagExpression,
            RtTagExpression tagComparation,
            int? discardValue = null,
            double? tolerance = null,
            Func<ITagRuleDefinition<TEvent>, bool> action = null)
        {
            var rule = new TagRuleDefinition<TEvent>(eventId, ruleCheckType, tagExpression, tagComparation, 
                discardValue ?? defaultDiscardValue, tolerance?? defaultTolerance, action ?? defaultAction);

            tagRules.Add(rule);
        } 

        public void EvaluateChanges()
        {
            foreach (var rule in tagRules)
            {
                rule.EvaluateChange();
            }
        }
    }
}
