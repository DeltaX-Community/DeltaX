using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaX.RealTime.ProcessInfo
{ 
    public abstract class ProcessInfoStatisticsBase
    {
        class StatisticsProps
        {
            public IRtTag Tag;
            public PropertyInfo Info;
            public object PrevValue;
        }

        private IRtConnector connector;
        private string prefixTagName;
        private string processName;
        private Dictionary<string, StatisticsProps> tags;

        public ProcessInfoStatisticsBase(
            IRtConnector connector,
            string processName = null)
        {
            this.tags = new Dictionary<string, StatisticsProps>();
            this.connector = connector;
            if (processName == null)
            {
                processName = Process.GetCurrentProcess().ProcessName;
                if (processName.StartsWith("DeltaX."))
                {
                    processName = processName.Substring("DeltaX.".Length);
                }
            }
            this.processName = processName;
            this.prefixTagName = $"process_info/{this.processName}";
            this.Startup = DateTime.Now;
            this.ConnectorStr = connector.ToString();
        }

        public string ConnectorStr { get; private set; }
        public DateTimeOffset Startup { get; private set; }
        public double AliveMs => (DateTime.Now - Startup).TotalSeconds;

        private void ParseProperties()
        {
            var type = this.GetType();
            var p = type.GetProperties();

            foreach (var prop in p)
            {
                var tag = connector.GetOrAddTag($"{prefixTagName}/{prop.Name}");
                tags[prop.Name] = new StatisticsProps() { Tag = tag, Info = prop, PrevValue = null };
            }
        }

        public IRtTag GetTag(string propName)
        {
            return connector.GetOrAddTag($"{prefixTagName}/{propName}");
        }

        public virtual Task SetValuesFromPropertiesAsync(IEnumerable<string> tagsName = null)
        {
            return Task.Run(() =>
            {
                if (!connector.IsConnected)
                {
                    return;
                }

                var filteredTags = tagsName?.Any() == true
                    ? tags.Where(t => tagsName.Contains(t.Key)).Select(t => t.Value).ToList()
                    : tags.Values.ToList();

                foreach (var tag in filteredTags)
                {
                    var value = tag.Info.GetValue(this, null);
                    if ((value == null && tag.PrevValue == null) || Object.Equals(value, tag.PrevValue))
                    {
                        continue; // Skip set value
                    }
                    bool res = false;

                    if (value == null)
                    {
                        res = tag.Tag.SetText(string.Empty);
                    }
                    else if (value is string str)
                    {
                        res = tag.Tag.SetText(str);
                    }
                    else if (value is byte[] bry)
                    {
                        res = tag.Tag.SetBinary(bry);
                    }
                    else if (value is DateTimeOffset dt)
                    {
                        res = tag.Tag.SetDateTime(dt);
                    }
                    else
                    {
                        try
                        {
                            var val = (double)value;
                            res = tag.Tag.SetNumeric(val);
                        }
                        catch
                        {
                            res = tag.Tag.SetText(value.ToString());
                        }
                    }

                    if (res)
                    {
                        tag.PrevValue = value;
                    }
                }
            });
        }

        public Task LoopPublishStatistics(
            TimeSpan? publishInterval = null,
            CancellationToken? cancellation = null)
        {
            publishInterval ??= TimeSpan.FromSeconds(60);
            cancellation ??= CancellationToken.None;

            ParseProperties();

            return Task.Run(async () =>
            {
                while (!cancellation.Value.IsCancellationRequested)
                {
                    await SetValuesFromPropertiesAsync();
                    await Task.Delay(publishInterval.Value);
                }
            });
        }
    }


}
