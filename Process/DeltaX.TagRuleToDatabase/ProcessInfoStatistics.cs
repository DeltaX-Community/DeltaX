﻿using DeltaX.RealTime.Interfaces;
using DeltaX.RealTime.ProcessInfo;
using System;

public class ProcessInfoStatistics : ProcessInfoStatisticsBase
{
    public ProcessInfoStatistics(IRtConnector connector)
        : base(connector, "TagRuleToDatabase")
    {
    }

    public DateTimeOffset ConnectedDateTime { get; set; }
    public DateTimeOffset RunningDateTime { get; set; }
    public int RulesCount { get; set; }
    public string RuleEventId { get; set; }
    public short ActionExecuted { get; set; }
    public int ActionAffectedRows { get; set; } 
    public bool WaitingTags { get; set; } 
}
