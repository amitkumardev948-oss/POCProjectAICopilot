namespace Engineering_IntelligenceTools.Models.Analysis;

public class ImpactSummary
{
    public int FilesAffected { get; init; }
    public bool CriticalPathTouched { get; init; }
    public List<string> CriticalFiles { get; init; } = new();
}
