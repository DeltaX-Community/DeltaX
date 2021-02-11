namespace DeltaX.Modules.TagRuleEvaluator
{
    using DeltaX.RealTime.Interfaces;
    using DeltaX.RealTime.RtExpression;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;


    public class TagRuleChangeEvaluator<TEvent>
    {
        private List<ITagRuleDefinition<TEvent>> tagRules;
        private Func<ITagRuleDefinition<TEvent>, bool> defaultAction = null;
        private readonly double defaultTolerance; 
        private ILogger logger;

        public TagRuleChangeEvaluator(
            Func<ITagRuleDefinition<TEvent>, bool> defaultAction = null,
            double defaultTolerance = 0.01, 
            ILoggerFactory loggerFactory = null)
        {
            loggerFactory ??= Configuration.Configuration.DefaultLoggerFactory;
            this.logger = loggerFactory.CreateLogger("TagRuleChangeEvaluator");
            this.tagRules = new List<ITagRuleDefinition<TEvent>>();
            this.defaultAction = defaultAction;
            this.defaultTolerance = defaultTolerance; 
        }

        public void AddRule(
            TEvent eventId,
            TagRuleCheckType ruleCheckType,
            IRtTag tagExpression,
            IRtTag tagComparation, 
            double? tolerance = null,
            Func<ITagRuleDefinition<TEvent>, bool> action = null)
        {
            var rule = new TagRuleDefinition<TEvent>(eventId, ruleCheckType, tagExpression, tagComparation,
                tolerance?? defaultTolerance, action ?? defaultAction, logger);

            tagRules.Add(rule);

            logger?.LogInformation("Add Rule [{0}] {1} TagName:[{2}] TagExpression:[{3}] TagComparation:{4} Status:{5} Updated:{6} ",
                rule.EventId, rule.RuleCheckType, rule.TagExpression.TagName, rule.TagExpression, rule.TagComparation, 
                rule.TagExpression.Status, rule.TagExpression.Updated);
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
