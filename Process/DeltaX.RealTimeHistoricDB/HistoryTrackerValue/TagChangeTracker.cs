using DeltaX.CommonExtensions; 
using DeltaX.RealTime.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

class TagChangeTracker
{
    private ConcurrentQueue<HistoricTagValueRecord> historicValues;
    private ConcurrentQueue<HistoricTagValueRecord> historicValuesCache;

    internal TagChangeTracker(string tagName, IRtTag tag, int tagId)
    {
        this.historicValues = new ConcurrentQueue<HistoricTagValueRecord>();
        this.historicValuesCache = new ConcurrentQueue<HistoricTagValueRecord>();
        this.TagName = tagName;
        this.TagId = tagId;
        this.Enable = true;
        this.tag = tag;
        this.IsChanged();
    }

    internal void AppendLastValue()
    {
        historicValues.Enqueue(GetLastHistoricTagValue());
    }

    public HistoricTagValueRecord GetLastHistoricTagValue(DateTime? miniumUpdated = null)
    {
        double updated = miniumUpdated.HasValue && this.Updated < miniumUpdated
            ? miniumUpdated.Value.ToUnixTimestamp()
            : this.Updated.ToUnixTimestamp();

        return new HistoricTagValueRecord
        {
            TagId = this.TagId,
            Value = this.Value,
            Updated = updated
        };
    }

    public IEnumerable<HistoricTagValueRecord> GetAndCleanHistoricTagValues()
    {
        lock (historicValues)
        {
            historicValuesCache = new ConcurrentQueue<HistoricTagValueRecord>(historicValues);
            historicValues.Clear();
            return historicValuesCache;
        }
    }

    public IEnumerable<HistoricTagValueRecord> GetHistoricTagValues()
    {
        lock (historicValues)
        {
            var combined = new List<HistoricTagValueRecord>(historicValuesCache);
            combined.AddRange(historicValues);
            if (!combined.Any())
            {
                combined.Add(GetLastHistoricTagValue());
            }
            return combined;
        }
    }

    private IRtTag tag;
    public int TagId { get; private set; }
    public string TagName { get; private set; }
    public bool Enable { get; private set; }
    public DateTime Updated { get; set; }
    public string Value { get; set; }
    public string PrevValue { get; set; }
    public bool Status { get; set; }

    internal bool IsChanged()
    {
        PrevValue = Value;
        Status = tag.Status;
        Updated = tag.Updated;
        Value = tag.Value.Text;

        return Status && PrevValue != Value;
    }
}