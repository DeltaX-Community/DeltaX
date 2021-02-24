using DeltaX.CommonExtensions;
using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


public class RealTimeHistoricDbService
{
    private ILogger logger;
    private IRtConnector connector;
    private readonly IHistoricRepository repository;
    private readonly ProcessInfoStatistics processInfo;
    private Queue<List<TagChangeTracker>> wsTags;
    private TimeSpan checkTagChangeInterval;
    private TimeSpan saveChangeInterval;
    private int daysPresistence;
    private TagChangeTrackerManager tagChangeManager;
    private DateTime lastCommit;


    public RealTimeHistoricDbService(
        IRtConnector connector,
        IHistoricRepository repository,
        ILoggerFactory loggerFactory,
        ProcessInfoStatistics processInfo,
        IOptions<RealTimeHistoryDBConfiguration> options)
    {
        this.logger = loggerFactory.CreateLogger(nameof(WorkerService));
        this.connector = connector;
        this.repository = repository;
        this.processInfo = processInfo;
        this.wsTags = new Queue<List<TagChangeTracker>>();
        this.tagChangeManager = new TagChangeTrackerManager();
        this.lastCommit = DateTime.Now;

        var intervalRefresh = options.Value.CheckTagChangeIntervalMilliseconds ?? 1000;
        var intervalCommit = options.Value.SaveChangeIntervalSeconds ?? 5 * 60;
        this.checkTagChangeInterval = TimeSpan.FromMilliseconds(intervalRefresh > 100 ? intervalRefresh : 100);
        this.saveChangeInterval = TimeSpan.FromSeconds(intervalCommit > 2 ? intervalCommit : 2);

        this.daysPresistence = options.Value.DaysPresistence ?? 30;

        logger.LogInformation("Initialize RealTimeHistoricDbWorker, checkTagChangeInterval time:{checkTagChangeInterval} " +
            "saveChangeInterval time:{saveChangeInterval} daysPresistence:{daysPresistence}",
            checkTagChangeInterval, saveChangeInterval, daysPresistence);
    }

    public List<HistoricTagRecord> GetTopics()
    {
        return repository.GetListHistoricTags();
    }

    public List<HistoricTagValueDto> GetTagHistory(
        string tagName,
        DateTime beginDateTime,
        DateTime endDateTime,
        int maxPoints = 1000,
        string prevValue = null,
        bool strictMode = false)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        List<HistoricTagValueDto> result;
        var tag = tagChangeManager.GetFirst(tagName);
        if (tag == null)
        {
            AddTagsFromKnownTopics();
            tag = tagChangeManager.GetFirst(tagName)
                ?? throw new ArgumentException("TagName not found!");
        }

        var memValues = tag.GetHistoricTagValues()
            .Where(v => v.Updated >= beginDateTime.ToUnixTimestamp() && v.Updated <= endDateTime.ToUnixTimestamp())
            .Select(v => new HistoricTagValueDto { Updated = v.Updated.FromUnixTimestamp(), Value = v.Value })
            .OrderBy(v => v.Updated)
            .ToList();

        var beginMemValue = memValues.FirstOrDefault();
        if (beginMemValue != null)
        {
            if (beginDateTime < beginMemValue.Updated)
            {
                var dbValues = repository.GetTagHistory(tagName, beginDateTime.ToUnixTimestamp(), beginMemValue.Updated.ToUnixTimestamp(), maxPoints, prevValue);
                result = dbValues.Union(memValues).ToList();
            }
            else
            {
                result = memValues;
            }
        }
        else
        {
            result = repository.GetTagHistory(tagName, beginDateTime.ToUnixTimestamp(), endDateTime.ToUnixTimestamp(), maxPoints, prevValue);
        }

        result = strictMode
            ? NormalizeValues(result, beginDateTime.ToUnixTimestamp(), endDateTime.ToUnixTimestamp(), maxPoints)
            : result;

        watch.Stop();
        processInfo.GetTag("MethodGetTagHistory").SetJson(new
        {
            tagName,
            beginDateTime,
            endDateTime,
            maxPoints,
            prevValue,
            strictMode,
            resultCount = result.Count(),
            elapsedMilliseconds = watch.ElapsedMilliseconds
        });

        return result;
    }

    private List<HistoricTagValueDto> NormalizeValues(
        List<HistoricTagValueDto> items,
        double beginDT,
        double endDT,
        int maxPoints = 10000)
    {
        var results = new List<HistoricTagValueDto>();
        var paso = Math.Round((endDT - beginDT) / (maxPoints - 1), 5);
        var t = beginDT;
        string prevValue = null;

        while (t <= endDT)
        {
            foreach (var item in items.ToList())
            {
                if (item.Updated > t.FromUnixTimestamp())
                    break;
                prevValue = item.Value;
                items.Remove(item);
            }

            results.Add(new HistoricTagValueDto { Updated = t.FromUnixTimestamp(), Value = prevValue });
            t = Math.Round(t + paso, 5);

            if (Math.Abs(endDT - t) < 0.001)
                t = endDT;
        }

        return results;
    }


    private void AddTagsFromKnownTopics()
    {
        lock (this)
        {
            var actualTopics = tagChangeManager.GetAllTags().Select(t => t.TagName);
            var tagsToAdd = connector.KnownTopics.Where(t => !actualTopics.Contains(t));

            processInfo.ConnectorKnownTopics = connector.KnownTopics.Count();
            processInfo.TagsCount = actualTopics.Count() + tagsToAdd.Count();

            if (!tagsToAdd.Any())
            {
                return;
            }

            var tagsDbAdded = repository.GetInsertTags(tagsToAdd);
            foreach (var row in tagsDbAdded)
            {
                tagChangeManager.AddTagIfNotExist(connector, row.TagName, row.Id);
            }
        }
    }

    private void CommitTagsChanged()
    {
        lock (this)
        {
            var tagsValues = new List<HistoricTagValueRecord>();

            foreach (var tag in tagChangeManager.GetAllTags())
            {
                var values = tag.GetAndCleanHistoricTagValues();
                if (values.Any())
                {
                    tagsValues.AddRange(values);
                }
                else if (tag.Status)
                {
                    tagsValues.Add(tag.GetLastHistoricTagValue(lastCommit));
                }
            }

            if (tagsValues.Any())
            {
                logger.LogDebug("Save Database {0} elements", tagsValues.Count());
                var insertedRows = repository.SaveHistoricTagValues(tagsValues);
                processInfo.SavedRows += insertedRows;
                logger.LogInformation("Save Database: Has been saved {insertedRows} elements!", insertedRows);
            }

            lastCommit = DateTime.Now;

            GC.Collect();
        }
    }

    private Task LoopSaveDataBaseChangesAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            logger.LogInformation("Execution SaveDataBaseChanges Started: {time}", DateTimeOffset.Now);

            AddTagsFromKnownTopics();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(saveChangeInterval, stoppingToken);
                CommitTagsChanged();
                AddTagsFromKnownTopics();
            }

            logger.LogInformation("Execution SaveDataBaseChanges Finished: {time}", DateTimeOffset.Now);
        });
    }

    private Task LoopEnqueueTagsChangedAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            logger.LogInformation("Execution EnqueueTagsChanged Started: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                var count = tagChangeManager.EnqueueTagsChanged();
                processInfo.RunningDateTime = DateTime.Now; 
                processInfo.Changed += count;
                await Task.Delay(checkTagChangeInterval, stoppingToken);
            }

            logger.LogInformation("Execution EnqueueTagsChanged Finished: {time}", DateTimeOffset.Now);
        });
    }

    private Task LoopDeleteOldsHistoricTagValuesAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            logger.LogInformation("Execution DeleteOldsHistoricTagValues Started: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(saveChangeInterval * 2, stoppingToken);
                var deletedRows = repository.DeleteOldsHistoricTagValues(daysPresistence);
                processInfo.DeletedRows += deletedRows;
            }

            logger.LogInformation("Execution DeleteOldsHistoricTagValues Finished: {time}", DateTimeOffset.Now);
        });
    }

    public int CreateTables()
    {
        return repository.CreateTables();
    }

    public Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Wait connect with {connector}...", connector);
        
        connector.Connected += (s, e) =>
        {
            processInfo.ConnectedDateTime = DateTime.Now;
        };
        connector.ConnectAsync(stoppingToken).Wait();

        logger.LogInformation("Load all configured tags...", connector);
        var tagsDbAdded = repository.GetListHistoricTags();
        foreach (var row in tagsDbAdded.Where(r => r.Enable))
        {
            tagChangeManager.AddTagIfNotExist(connector, row.TagName, row.Id);
        }
        processInfo.TagsCount += tagsDbAdded.Count();

        return Task.WhenAny(
            processInfo.LoopPublishStatistics(TimeSpan.FromSeconds(30), stoppingToken),
            LoopDeleteOldsHistoricTagValuesAsync(stoppingToken),
            LoopSaveDataBaseChangesAsync(stoppingToken),
            LoopEnqueueTagsChangedAsync(stoppingToken))
            .ContinueWith((t) =>
            {
                if (t.Result.IsFaulted)
                {
                    logger.LogError(t.Result.Exception, "Execution RealTimeHistoricDbWorker Stoped: {time}", DateTimeOffset.Now);
                    Environment.Exit(-1);
                }
                else
                {
                    logger.LogWarning("Execution RealTimeHistoricDbWorker Stoped: {time}", DateTimeOffset.Now);
                }
            });
    }
} 