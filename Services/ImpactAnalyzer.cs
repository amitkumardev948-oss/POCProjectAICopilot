using Engineering_IntelligenceTools.Configuration;
using Engineering_IntelligenceTools.Models.Analysis;
using Engineering_IntelligenceTools.Models.GitHub;
using Engineering_IntelligenceTools.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Engineering_IntelligenceTools.Services;

public class ImpactAnalyzer : IImpactAnalyzer
{
    private readonly string[] _criticalPathMarkers;

    public ImpactAnalyzer(IOptions<GitHubOptions> options)
    {
        _criticalPathMarkers = options.Value.CriticalPathMarkers;
    }

    public ImpactSummary Analyze(IReadOnlyList<ChangedFile> files)
    {
        var criticalFiles = files
            .Where(f => _criticalPathMarkers.Any(marker =>
                f.Path.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            .Select(f => f.Path)
            .ToList();

        return new ImpactSummary
        {
            FilesAffected = files.Count,
            CriticalPathTouched = criticalFiles.Count > 0,
            CriticalFiles = criticalFiles
        };
    }
}
