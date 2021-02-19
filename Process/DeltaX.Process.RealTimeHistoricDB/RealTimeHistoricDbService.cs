namespace DeltaX.Process.RealTimeHistoricDB
{
    using DeltaX.CommonExtensions;
    using DeltaX.Process.RealTimeHistoricDB.Configuration;
    using DeltaX.Process.RealTimeHistoricDB.HistoryTrackerValue;
    using DeltaX.Process.RealTimeHistoricDB.Records;
    using DeltaX.Process.RealTimeHistoricDB.Repositories;
    using DeltaX.RealTime.Interfaces;
    using Microsoft.Extensions.Hosting;
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
            IOptions<RealTimeHistoryDBConfiguration> options)
        {
            this.logger = loggerFactory.CreateLogger(nameof(RealTimeHistoricDbWorker));
            this.connector = connector;
            this.repository = repository;
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


        public List<HistoricTagValueDto> GetTagHistory(
            string tagName,
            DateTime beginDateTime,
            DateTime endDateTime,
            int maxPoints = 1000,
            string prevValue = null,
            bool strictMode = false)
        {
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

            return strictMode
                ? NormalizeValues(result, beginDateTime.ToUnixTimestamp(), endDateTime.ToUnixTimestamp(), maxPoints)
                : result;
        }

        List<HistoricTagValueDto> NormalizeValues(
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
                    else
                    {
                        tagsValues.Add(tag.GetLastHistoricTagValue(lastCommit));
                    }
                }

                if (tagsValues.Count > 0)
                {
                    logger.LogDebug("Save Database {0} elements", tagsValues.Count);
                    var insertedRows = repository.SaveHistoricTagValues(tagsValues);
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
                    tagChangeManager.EnqueueTagsChanged();
                    await Task.Delay(checkTagChangeInterval, stoppingToken);
                }

                logger.LogInformation("Execution EnqueueTagsChanged Finished: {time}", DateTimeOffset.Now);
            });
        }

        private Task LoopDeleteOldsHistoricTagValuesAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                logger.LogInformation("Execution EnqueueTagsChanged Started: {time}", DateTimeOffset.Now);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(saveChangeInterval * 2, stoppingToken);
                    repository.DeleteOldsHistoricTagValues(daysPresistence);
                }

                logger.LogInformation("Execution EnqueueTagsChanged Finished: {time}", DateTimeOffset.Now);
            });
        }

        public int CreateTables()
        {
            return repository.CreateTables();
        }

        public Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.WhenAny(
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
}