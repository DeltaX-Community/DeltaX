public class TagRuleChangeConfig
{
    public string RealTimeConnectorSectionName { get; set; } 
    public double LoopEvaluateIntervalMilliseconds { get; set; }
    public double DefaultTolerance { get; set; }
    public RuleConfig[] Rules { get; set; }
}
