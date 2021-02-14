using DeltaX.CommonExtensions;
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
using System.Threading;
using System.Threading.Tasks;

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

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        return connector.ConnectAsync(cancellationToken);
    }

    public (IRtValue value, bool status, string name) ReadValue(string readExpression, ITagRuleDefinition<string> arg)
    {
        IRtTag tagRead;
        switch (readExpression)
        {
            case "TagExpression":
                tagRead = arg.TagExpression;
                return (tagRead.Value, tagRead.Status, tagRead.ToString()); 
            case "PrevValue":
                return (RtValue.Create(arg.PrevValue), true, readExpression); 
            case "Value":
                return (RtValue.Create(arg.Value), true, readExpression);
            case "UpdatedUnixTimestamp":
                return (RtValue.Create(arg.Updated.ToUnixTimestamp()), true, "UpdatedUnixTimestamp");
            default:
                tagRead = GetTag(readExpression);
                return (tagRead.Value, tagRead.Status, tagRead.ToString()); 
        } 
    }

    private bool ActionOnChange(ITagRuleDefinition<string> arg)
    {
        var rule = settings.Rules.First(r => r.EventId == arg.EventId);

        var read = ReadValue(rule.WriteValueExpression.Trim(), arg);
        var tagWrite = connector.GetOrAddTag(rule.WriteTag);

        logger?.LogInformation("Action [Event={0} Type={1}]. Write Tag:[{2} Value={3} Status={4}] Whit [<{5}> Value:{6} Status:{7}]",
            arg.EventId, arg.RuleCheckType, tagWrite.TagName, tagWrite.Value, tagWrite.Status, read.name, read.value, read.status);

        if (read.status)
        {
            return tagWrite.Set(read.value);
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
            tagRuleChangeEvaluator.AddRule(rule.EventId, rule.CheckType, GetTag(rule.TagExpression), rule.Tolerance, rule.DiscardValue);
        }
    }


    public IRtTag GetTag(string expresionString)
    { 
        IRtTag tag;
        if (!cacheTags.TryGetValue(expresionString, out tag))
        {
            tag = RtTagExpression.AddExpression(connector, expresionString); 
            cacheTags[expresionString] = tag;
        }
        return tag;
    }

    public void EvaluateChanges()
    {
        tagRuleChangeEvaluator.EvaluateChanges();
    }     
}