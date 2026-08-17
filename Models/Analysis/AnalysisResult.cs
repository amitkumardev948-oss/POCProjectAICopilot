using System.Text.Json.Serialization;
using Engineering_IntelligenceTools.Models.Enums;

namespace Engineering_IntelligenceTools.Models.Analysis;
public class AnalysisResult
{
    public required string RepoName { get; init; }
    public int? PullRequestNumber { get; init; }
    public required string BaseSha { get; init; }
    public required string HeadSha { get; init; }

    public List<string> Languages { get; init; } = new();

    public ComplexityMetrics Complexity { get; init; } = new();

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RiskLevel Risk { get; init; }

    public DependencyChanges Dependencies { get; init; } = new();

    public List<CodeSmell> CodeSmells { get; init; } = new();

    public ImpactSummary Impact { get; init; } = new();

    public List<FileChangeSummary> Files { get; init; } = new();

    public List<string> Recommendations { get; init; } = new();

    /// <summary>
    /// Roslyn AST-based metrics for changed .cs files, if any were present.
    /// Empty for non-C# repos or PRs that touch no C# files.
    /// </summary>
    public List<RoslynFileMetrics> RoslynMetrics { get; init; } = new();

    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
