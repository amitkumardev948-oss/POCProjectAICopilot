namespace Engineering_IntelligenceTools.Models.Enums;

/// <summary>
/// Overall risk classification produced by the risk engine for a PR / push.
/// </summary>
public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
