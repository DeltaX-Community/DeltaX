namespace DeltaX.RealTime.RtMemoryMapped
{
    using DeltaX.CommonExtensions;
    using DeltaX.MemoryMappedRecord;
    using DeltaX.RealTime.Interfaces;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class RtConnectorMemoryMapped : RtConnectorBase, IRtConnector
    {  
        private readonly ILogger logger; 
        private readonly HashSet<RtTagMemoryMapped> rtTags;
        private HashSet<string> extraTopics;
        private IKeyValueMemory keyValueMemory;
        private readonly bool syncWithExistentTopics;

        public static IRtConnector BuildFromFactory(
            IConfigurationSection configuration, 
            ILoggerFactory loggerFactory = null)
        {
            var config = new KeyValueMemoryConfiguration(configuration);
            var keyValueMemory = KeyValueMemory.Build(config);
            var syncWithExistentTopics = configuration?.GetValue<bool>("SyncWithExistentTopics", false) ?? false;
            return new RtConnectorMemoryMapped(keyValueMemory, syncWithExistentTopics, loggerFactory);
        }

        public static IRtConnector Build(
            KeyValueMemoryConfiguration configuration,
            bool syncWithExistentTopics = false,
            ILoggerFactory loggerFactory = null)
        {
            var keyValueMemory = KeyValueMemory.Build(configuration, loggerFactory);
            return new RtConnectorMemoryMapped(keyValueMemory, syncWithExistentTopics, loggerFactory);
        }

        private RtConnectorMemoryMapped(
            IKeyValueMemory keyValueMemory,
            bool syncWithExistentTopics = false,
            ILoggerFactory loggerFactory = null)
        {
            loggerFactory = loggerFactory ?? Configuration.Configuration.DefaultLoggerFactory;
            this.rtTags = new HashSet<RtTagMemoryMapped>();
            this.logger = loggerFactory.CreateLogger($"{nameof(RtConnectorMemoryMapped)}");
            this.keyValueMemory = keyValueMemory;
            this.syncWithExistentTopics = syncWithExistentTopics;
            this.extraTopics = new HashSet<string>();

            this.keyValueMemory.KeysChanged += KeyValueMemory_KeysChanged;
            LoadTopics();
        }

        private void KeyValueMemory_KeysChanged(object sender, List<string> e)
        {
            LoadTopics();
        }

        private void LoadTopics()
        {
            lock (rtTags)
            {
                if (syncWithExistentTopics)
                {
                    foreach (var topic in keyValueMemory.Keys.Where(k => !rtTags.Any(t => t.TagName == k)))
                    {
                        AddTag(topic, topic, null);
                    }
                }

                foreach (var topic in keyValueMemory.Keys.Where(k => !extraTopics.Contains(k)))
                {
                    extraTopics.Add(topic);
                }
            }
        }

        public override IEnumerable<string> TagNames
        {
            get
            {
                lock (rtTags) return rtTags.Select(t => t.TagName).ToList();
            }
        }

        public override IEnumerable<string> KnownTopics
        {
            get
            {
                lock (rtTags) return extraTopics.Union(rtTags.Select(t => t.Topic)).ToHashSet();
            }
        }


        public override bool IsConnected => keyValueMemory != null;

        public override IRtTag AddTag(string tagName, string topic, IRtTagOptions options)
        {  
            RtTagMemoryMapped t = new RtTagMemoryMapped(this, tagName, topic, options);
            lock (rtTags)
            {
                rtTags.Add(t);
            }

            logger?.LogDebug("AddTag TagName:{0}", t.TagName);
            return t;
        }

        public override Task ConnectAsync(CancellationToken? cancellationToken = null)
        {
            return Task.CompletedTask;
        }

        public override bool Disconnect()
        { 
            keyValueMemory?.Flush();
            keyValueMemory?.Dispose();
            keyValueMemory = null;

            lock (rtTags)
            {
                foreach (var tag in rtTags)
                {
                    tag.RaiseStatusChanged(IsConnected);
                }
            }
            RaiseOnDisconnect(false);
            return true;
        }

        public override IRtTag GetTag(string tagName)
        {
            lock (rtTags)
            {
                return rtTags.FirstOrDefault(t => t.TagName == tagName);
            }
        }

        public void RemoveTag(IRtTag tag)
        {
            if (!syncWithExistentTopics)
            {
                lock (rtTags) rtTags.Remove(tag as RtTagMemoryMapped);
            }
            logger?.LogDebug("RemoveTag TagName:{0}", tag.TagName);
        }

        internal void ReadAndRaiseTagOnUpdated(RtTagMemoryMapped tag, DateTime lastUpdated)
        {
            var updated = ReadTagUpdated(tag.Topic);
            if (updated.HasValue && lastUpdated != updated.Value)
            {
                tag.RaiseOnUpdatedValue(ReadTagValue(tag.Topic), updated, true); 
                RaiseOnUpdatedValue(tag);
            } 
        }

        internal void ReadAndRaiseTagStatusChanged(RtTagMemoryMapped tag, bool lastStatus)
        {
            var status = ReadTagStatus(tag.Topic);
            if (lastStatus != status)
            {
                tag.RaiseStatusChanged( status); 
            }
        }

        internal IRtValue ReadTagValue(string topic)
        {
            var value = keyValueMemory?.GetValue(topic);
            return value != null ? RtValue.Create(value) : null;
        }

        internal DateTime? ReadTagUpdated(string topic)
        {
            var updated = keyValueMemory?.GetUpdated(topic);
            return updated > 1 ? updated?.FromUnixTimestamp() : null;
        }

        internal bool ReadTagStatus(string topic)
        {
            return keyValueMemory?.Keys?.Contains(topic) ?? false;
        }

        public override bool WriteValue(IRtTag tag, IRtValue value)
        {
            if (IsConnected && keyValueMemory != null)
            {
                var writed = keyValueMemory.SetValue(tag.Topic, value.Binary, true);
                if (writed)
                {
                    lock (rtTags)
                    {
                        foreach (var _tag in rtTags.Where(t => t.Topic == tag.Topic))
                        {
                            _tag.RaiseOnSetValue(value);
                        }
                    }

                    RaiseOnSetValue(tag);

                    logger?.LogDebug("WriteValue TagName:{0} return:{1}", tag.TagName, true);
                    return true;
                }
            }

            logger?.LogWarning("WriteValue TagName:{0} return:{1}", tag.TagName, false);
            return false;
        }

        public override bool WriteValue(string topic, IRtValue value, IRtTagOptions options = null)
        {
            return WriteValue(new RtTagMemoryMapped(this, topic, topic, options), value);
        }
    }
}
