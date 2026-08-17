namespace Engineering_IntelligenceTools.Models.Analysis;
public class RoslynFileMetrics
{
    public required string FilePath { get; init; }
    public int CyclomaticComplexity { get; init; }
    public int MaxNestingDepth { get; init; }
    public int MethodCount { get; init; }
    public int LongestMethodLines { get; init; }
    public List<RoslynFinding> Findings { get; init; } = new();
}
public class RoslynFinding
{
    public required string Type { get; init; }
    public required string Description { get; init; }
    public int Line { get; init; }
}
