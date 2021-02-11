using DeltaX.MemoryMappedRecord;
using DeltaX.Modules.TagRuleEvaluator;
using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using DeltaX.RealTime.RtExpression;
using DeltaX.RealTime.RtMemoryMapped;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

public class TagRuleChangeExecutor
{
    private TagRuleChangeConfig settings;
    private TagRuleChangeEvaluator<string> tagRuleChangeEvaluator;
    private readonly ILogger<TagRuleChangeExecutor> logger;
    private readonly RtConnectorFactory connectorFactory;
    private IRtConnector connector;
    private Dictionary<string, IRtTag> cacheTags;

    public TagRuleChangeExecutor(
        IOptions<TagRuleChangeConfig> settings,
        ILogger<TagRuleChangeExecutor> logger,
        RtConnectorFactory connectorFactory)
    {
        this.settings = settings.Value;
        this.logger = logger;
        this.connectorFactory = connectorFactory;
        cacheTags = new Dictionary<string, IRtTag>();
        tagRuleChangeEvaluator = new TagRuleChangeEvaluator<string>(ActionOnChange, this.settings.DefaultTolerance);
        connector = connectorFactory.GetConnector(this.settings.RealTimeConnectorSectionName);
    }

    private bool ActionOnChange(ITagRuleDefinition<string> arg)
    {
        var rule = settings.Rules.First(r => r.EventId == arg.EventId);

        IRtTag tagRead;
        if (rule.WriteValueExpression.Trim() == "TagExpression")
        { 
            tagRead = arg.TagExpression; 
        }
        else if (rule.WriteValueExpression.Trim() == "TagComparation")
        {
            tagRead = arg.TagComparation;
        }
        else
        {
            tagRead = GetTag(rule.WriteValueExpression);
        }

        var tagWrite = connector.GetOrAddTag(rule.WriteTag);
        var value = tagRead.Value;

        logger?.LogInformation("Action [Event={0} Type={1}]. Write Tag:[{2} Value={3} Status={4}] with ValueExpression:[<{5}> Value:{6} Status:{7}]",
            arg.EventId, arg.RuleCheckType, tagWrite.TagName, tagWrite.Value, tagWrite.Status, tagRead, value, tagRead.Status);

        if (tagRead.Status)
        {
           return tagWrite.SetNumeric(value.Numeric);
        }
        return false;
    }

    public void LoadRules()
    {
        int count = 0;
        foreach (var rule in settings.Rules)
        {
            count++;
            rule.EventId = string.IsNullOrEmpty(rule.EventId) ? $"Event{count}" : rule.EventId;
            rule.TagComparation = string.IsNullOrEmpty(rule.TagComparation) ? $"0" : rule.TagComparation;
            tagRuleChangeEvaluator.AddRule(rule.EventId, rule.CheckType, GetTag(rule.TagExpression),
                GetTag(rule.TagComparation), rule.Tolerance);
        }
    }


    public IRtTag GetTag(string expresionString)
    { 
        IRtTag tag;
        if (!cacheTags.TryGetValue(expresionString, out tag))
        {
            tag = new RtTagExpression(connector, expresionString); 
            cacheTags[expresionString] = tag;
        }
        return tag;
    }

    public void EvaluateChanges()
    {
        tagRuleChangeEvaluator.EvaluateChanges();
    }     
}