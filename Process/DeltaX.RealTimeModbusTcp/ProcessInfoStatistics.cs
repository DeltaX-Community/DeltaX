using DeltaX.RealTime.Interfaces;
using DeltaX.RealTime.ProcessInfo;
using Microsoft.Extensions.Options;
using System;

public class ProcessInfoStatistics : ProcessInfoStatisticsBase
{
    public ProcessInfoStatistics(IRtConnector connector, IOptions<ModbusReadConfiguration> options)
        : base(connector, options?.Value?.ProcessInfoName)
    {
    }

    public DateTimeOffset ConnectedDateTime { get; set; }
    public DateTimeOffset RunningDateTime { get; set; } 
    public int ReadBlocks { get; set; } 
    public int TagsCount { get; set; } 
    public int ScanCounter { get; set; }
    public DateTimeOffset ScanDateTime { get; set; }
    public string ScanLastErrror { get; set; }   
    public int ScanRetry { get; set; }   
}
