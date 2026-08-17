using System.Collections.Concurrent;
using Engineering_IntelligenceTools.Models.Analysis;
using Engineering_IntelligenceTools.Services.Interfaces;

namespace Engineering_IntelligenceTools.Services;
public class InMemoryAnalysisResultStore : IAnalysisResultStore
{
    private readonly ConcurrentDictionary<string, AnalysisResult> _results = new();

    public void Save(AnalysisResult result)
    {
        _results[BuildKey(result.RepoName, result.PullRequestNumber)] = result;
    }

    public AnalysisResult? Get(string repoFullName, int? pullRequestNumber)
    {
        return _results.TryGetValue(BuildKey(repoFullName, pullRequestNumber), out var result)
            ? result
            : null;
    }

    private static string BuildKey(string repoFullName, int? pullRequestNumber) =>
        $"{repoFullName.ToLowerInvariant()}#{pullRequestNumber?.ToString() ?? "push"}";
}
