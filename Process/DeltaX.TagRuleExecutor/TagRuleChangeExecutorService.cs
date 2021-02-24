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

public class TagRuleChangeExecutorService
{
    private TagRuleChangeConfiguration settings;
    private TagRuleChangeEvaluator<string> tagRuleChangeEvaluator;
    private readonly ILogger logger;
    private readonly ProcessInfoStatistics processInfo;
    private IRtConnector connector;
    private Dictionary<string, IRtTag> cacheTags;

    public TagRuleChangeExecutorService(
        IOptions<TagRuleChangeConfiguration> settings,
        ILogger<TagRuleChangeExecutorService> logger,
        ProcessInfoStatistics statistics,
        IRtConnector connector)
    {
        this.settings = settings.Value;
        this.logger = logger;
        this.processInfo = statistics;
        this.connector = connector;
        this.cacheTags = new Dictionary<string, IRtTag>();
        this.tagRuleChangeEvaluator = new TagRuleChangeEvaluator<string>(ActionOnChange, this.settings.DefaultTolerance, this.settings.DefaultDiscardValue);
    }

    public (IRtValue value, bool status, string name) ReadValue(string readExpression, ITagRuleDefinition<string> arg)
    {
        if (readExpression.StartsWith("'") && readExpression.EndsWith("'"))
        {
            return (RtValue.Create(readExpression.Trim('\'')), true, readExpression);
        }

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
        var result = new List<bool>();
        var rule = settings.Rules.First(r => r.EventId == arg.EventId);

        foreach (var writeTagConfig in rule.WriteTags)
        {
            var read = ReadValue(writeTagConfig.Value.Trim(), arg);
            var tagWrite = connector.GetOrAddTag(writeTagConfig.Tag);

            logger?.LogInformation("Action [Event={0} Type={1}]. Write Tag:[{2} Value={3} Status={4}] Whit [<{5}> Value:{6} Status:{7}]",
                arg.EventId, arg.RuleCheckType, tagWrite.TagName, tagWrite.Value, tagWrite.Status, read.name, read.value, read.status);

            if (read.status)
            {
                result.Add(tagWrite.SetText(read.value.Text));
            }
        }

        processInfo.RuleEventId = arg.EventId;
        processInfo.ActionExecuted += 1;
        processInfo.ActionWriteTags = result.Count();
        processInfo.SetValuesFromPropertiesAsync().Wait();
        return result.Any();
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

        processInfo.RulesCount = count;
    }

    public void LoadTagsToCache()
    {
        var skipTags = new[] { "TagExpression", "PrevValue", "Value", "UpdatedUnixTimestamp" };
        cacheTags = settings.Rules
            .Select(r => r.TagExpression)
            .Union(settings.Rules.SelectMany(r => r.WriteTags.Select(t => t.Value)))
            .Except(skipTags)
            .ToHashSet()
            .ToDictionary(tagExpression => tagExpression, tagExpression => RtTagExpression.AddExpression(connector, tagExpression));
    }

    public Task WaitTagsStatusAsync(CancellationToken stoppingToken)
    {
        var tags = settings.Rules
            .Select(r => r.TagExpression)
            .Select(t => GetTag(t)).ToList();

        return Task.Run(async () =>
        {
            processInfo.WaitingTags = true;
            logger.LogInformation("Wait for read all tags status");

            await Task.Delay(200);
            var tagsNoOk = tags.Where(t => !t.Status).ToList();
            var countNoOk = 0;

            while (!stoppingToken.IsCancellationRequested && tagsNoOk.Any())
            {
                if (countNoOk != tagsNoOk.Count())
                {
                    countNoOk = tagsNoOk.Count();
                    foreach (var tag in tagsNoOk)
                    {
                        logger.LogWarning("Waiting for Tag:{tag} Status:{status}", tag.TagName, tag.Status);
                    }
                }

                tagsNoOk = tags.Where(t => !t.Status).ToList();
                await Task.Delay(1000, stoppingToken);
            }

            processInfo.WaitingTags = false;
            logger.LogInformation("All Tags with status true has been read.");
        });
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

    public Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var LoopEvaluateInterval = settings?.LoopEvaluateIntervalMilliseconds ?? 500;
        connector.Connected += (s, c) =>
        {
            processInfo.ConnectedDateTime = DateTime.Now;
        };

        connector.ConnectAsync(stoppingToken).Wait();
        var tagNow = connector.AddTag(settings.TagDateTimeNow);

        if (!stoppingToken.IsCancellationRequested)
        {
            LoadTagsToCache();
            LoadRules();
            processInfo.LoopPublishStatistics(TimeSpan.FromSeconds(10), stoppingToken);
            WaitTagsStatusAsync(stoppingToken).Wait(); 
        }

        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                processInfo.RunningDateTime = DateTime.Now;
                tagNow.SetDateTime(processInfo.RunningDateTime);
                tagRuleChangeEvaluator.EvaluateChanges();
                await Task.Delay(TimeSpan.FromMilliseconds(LoopEvaluateInterval), stoppingToken);
            }
        }).ContinueWith(t =>
        {
            logger.LogWarning("Process Stoped at: {time}", DateTimeOffset.Now.ToString("o"));
        });
    }
}