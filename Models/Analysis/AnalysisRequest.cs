namespace Engineering_IntelligenceTools.Models.Analysis;
public class AnalysisRequest
{
    public required string Owner { get; init; }
    public required string Repo { get; init; }
    public int? PullRequestNumber { get; init; }
    public string? BaseSha { get; init; }
    public string? HeadSha { get; init; }
}
