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
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class TagRuleToSqlService
{
    private TagRuleChangeConfiguration settings;
     

    private TagRuleChangeEvaluator<string> tagRuleChangeEvaluator;
    private readonly IDatabaseManager database;
    private readonly ILogger logger;
    private IRtConnector connector;
    private Dictionary<string, IRtTag> cacheTags;
    private static readonly Regex regParameters = new Regex(@"@\w+", RegexOptions.Compiled);

    public TagRuleToSqlService(
        IOptions<TagRuleChangeConfiguration> settings,
        IDatabaseManager database,
        ILogger<TagRuleToSqlService> logger,
        IRtConnector connector)
    {
        this.settings = settings.Value;
        this.database = database;
        this.logger = logger;
        this.connector = connector; 
        this.cacheTags = new Dictionary<string, IRtTag>();
        this.tagRuleChangeEvaluator = new TagRuleChangeEvaluator<string>(ActionOnChange, this.settings.DefaultTolerance, this.settings.DefaultDiscardValue);        
    }


    public (IRtValue value, bool status, string name, DateTime updated) ReadValue(string readExpression, ITagRuleDefinition<string> arg)
    {
        IRtTag tagRead;
        switch (readExpression)
        {
            case "TagExpression":
                tagRead = arg.TagExpression;
                return (tagRead.Value, tagRead.Status, tagRead.TagName, tagRead.Updated);
            case "PrevValue":
                return (RtValue.Create(arg.PrevValue), true, readExpression, arg.PrevUpdated);
            case "Value":
                return (RtValue.Create(arg.Value), true, readExpression, arg.Updated);
            default:
                tagRead = GetTag(readExpression);
                return (tagRead.Value, tagRead.Status, tagRead.TagName, tagRead.Updated);
        }
    }


    private Dictionary<string, object> GetSqlParameters(RuleConfiguration rule, string commandSql, ITagRuleDefinition<string> arg)
    {
        var result = new Dictionary<string, object>();
        var sqlArgs = rule.WriteSql.ReadTags.Select(rt => ReadValue(rt, arg)).ToList();
        var matches = regParameters.Matches(commandSql).Select(m => m.Value).ToList();

        var count = 0;
        foreach (var tag in sqlArgs)
        {
            result.Add($"@tag_{count}", tag.value.Text);
            result.Add($"@tag_{count}_numeric", tag.value.Numeric);
            result.Add($"@tag_{count}_name", tag.name);
            result.Add($"@tag_{count}_status", tag.status );
            result.Add($"@tag_{count}_updated", tag.updated);
            count++;
        }

        return result
            .Where(r => matches.Contains(r.Key))
            .ToDictionary(p => p.Key, p => p.Value);
    }

    private bool ActionOnChange(ITagRuleDefinition<string> arg)
    {
        logger?.LogInformation("Action for Event:[{0}] {1} TagName:[{2}] Expresion:[{3}] PrevValue:{4} Value:{5} Updated:{6}",
            arg.EventId, arg.RuleCheckType, arg.TagExpression.TagName, arg.TagExpression, arg.PrevValue, arg.Value, arg.Updated);

        var rule = settings.Rules.First(r => r.EventId == arg.EventId);

        var sql = string.Join("\n", rule.WriteSql.FormatSql);
        var parameters = GetSqlParameters(rule, sql, arg);

        logger.LogInformation("Try SaveToDb \nSql:{@sql} \nParameters:{@param}", sql, parameters);
        var result = database.SaveToDb(rule.WriteSql.ConnectionFactory, sql, rule.WriteSql.CommandType, parameters);
        logger.LogInformation("SaveToDb affected rows: {rows}", result);
        
        return true;
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

    public void LoadTagsToCache()
    {
        cacheTags = settings.Rules.Select(r => r.TagExpression)
            .Union(settings.Rules.SelectMany(r => r.WriteSql.ReadTags))
            .ToList()
            .ToDictionary(tagExpression => tagExpression, tagExpression => RtTagExpression.AddExpression(connector, tagExpression));
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
        connector.ConnectAsync(stoppingToken).Wait();

        LoadTagsToCache();
        LoadRules();

        var tagNow = connector.AddTag(settings.TagNowName);

        var taskInfo = Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogDebug("- Worker running at: {time}", DateTimeOffset.Now.ToString("o"));
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        });

        var taskEvaluator = Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                tagNow.SetDateTime(DateTime.Now);
                tagRuleChangeEvaluator.EvaluateChanges();
                await Task.Delay(TimeSpan.FromMilliseconds(LoopEvaluateInterval), stoppingToken);
            }
        });

        return Task.WhenAll(taskEvaluator)
            .ContinueWith(t =>
        {
            logger.LogWarning("Process Stoped at: {time}", DateTimeOffset.Now.ToString("o"));
        });
    }
}