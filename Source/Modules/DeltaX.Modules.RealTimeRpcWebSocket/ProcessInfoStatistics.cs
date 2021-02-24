namespace DeltaX.Modules.RealTimeRpcWebSocket
{
    using DeltaX.RealTime.Interfaces;
    using DeltaX.RealTime.ProcessInfo;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class ProcessInfoStatistics : ProcessInfoStatisticsBase
    {
        public ProcessInfoStatistics(IRtConnector connector)
            : base(connector)
        {
        }

        public DateTimeOffset ConnectedDateTime { get; set; }
        public DateTimeOffset RunningDateTime { get; set; }
        public int ConnectedClients { get; set; }
        public int TagsCount { get; set; }
        public int TagsChanged { get; set; }

        public override Task SetValuesFromPropertiesAsync(IEnumerable<string> tagsName = null) 
        {
            var result = base.SetValuesFromPropertiesAsync(tagsName);
            TagsChanged = 0;
            return result;
        }
    }
}