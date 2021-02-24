using DeltaX.RealTime.Interfaces;
using DeltaX.RealTime.ProcessInfo;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ProcessInfoStatistics : ProcessInfoStatisticsBase
{
    public ProcessInfoStatistics(IRtConnector connector)
        : base(connector, "RealTimeHistoricDB")
    {
    }

    public DateTimeOffset ConnectedDateTime { get; set; }
    public DateTimeOffset RunningDateTime { get; set; } 
    public int TagsCount { get; set; }
    public int ConnectorKnownTopics { get; set; }
    public int Changed { get; set; }
    public int DeletedRows { get; set; }
    public int SavedRows { get; set; } 

    public override Task SetValuesFromPropertiesAsync(IEnumerable<string> tagsName = null)
    {
        var result = base.SetValuesFromPropertiesAsync(tagsName);
        if (tagsName == null)
        {
            Changed = 0;
            DeletedRows = 0;
            SavedRows = 0;
        }
        return result;
    }
}
