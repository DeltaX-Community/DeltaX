public class  TagRuleToDatabaseConfiguration
{
    public string TagDateTimeNow { get; set; } = "now";
    public double LoopEvaluateIntervalMilliseconds { get; set; }
    public double DefaultTolerance { get; set; } = 0.001;
    public double DefaultDiscardValue { get; set; } = 0;
    public RuleConfiguration[] Rules { get; set; }
}
