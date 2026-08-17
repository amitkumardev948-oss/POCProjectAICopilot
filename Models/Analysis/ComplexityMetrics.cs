namespace Engineering_IntelligenceTools.Models.Analysis;
public class ComplexityMetrics
{
    public double Score { get; init; }

    public int TotalLinesChanged { get; init; }
    public int FilesChanged { get; init; }
    public int MaxNestingDepthDelta { get; init; }
    public double AverageChangeSizePerFile { get; init; }
}
