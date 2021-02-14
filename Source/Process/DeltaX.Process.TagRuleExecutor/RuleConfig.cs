﻿using DeltaX.Modules.TagRuleEvaluator;

public class RuleConfig
{
    public string EventId { get; set; }
    public TagRuleCheckType CheckType { get; set; }
    public string TagExpression { get; set; } 
    public double? Tolerance { get; set; }   
    public double? DiscardValue { get; set; }   
    public string WriteTag { get; set; }   
    public string WriteValueExpression { get; set; }   
}
