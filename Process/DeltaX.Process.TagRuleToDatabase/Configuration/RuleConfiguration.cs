using DeltaX.Modules.TagRuleEvaluator;

public class RuleConfiguration
{
    public string EventId { get; set; }
    public TagRuleCheckType CheckType { get; set; }
    public string TagExpression { get; set; }
    public double? Tolerance { get; set; }
    public double? DiscardValue { get; set; }
    public WriteSqlConfiguration WriteSql { get; set; }
}
