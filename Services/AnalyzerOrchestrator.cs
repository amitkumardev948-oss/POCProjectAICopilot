using Engineering_IntelligenceTools.Models.Analysis;
using Engineering_IntelligenceTools.Models.Enums;
using Engineering_IntelligenceTools.Services.Interfaces;

namespace Engineering_IntelligenceTools.Services;

public class AnalyzerOrchestrator : IAnalyzerOrchestrator
{
    private readonly ILanguageDetectionService _languageDetectionService;
    private readonly IComplexityAnalyzer _complexityAnalyzer;
    private readonly ICodeSmellDetector _codeSmellDetector;
    private readonly IDependencyAnalyzer _dependencyAnalyzer;
    private readonly IImpactAnalyzer _impactAnalyzer;
    private readonly IRiskEngine _riskEngine;
    private readonly IRoslynCSharpAnalyzer _roslynAnalyzer;
    private readonly IGitHubClientService _gitHubClientService;
    private readonly ILogger<AnalyzerOrchestrator> _logger;

    public AnalyzerOrchestrator(
        ILanguageDetectionService languageDetectionService,
        IComplexityAnalyzer complexityAnalyzer,
        ICodeSmellDetector codeSmellDetector,
        IDependencyAnalyzer dependencyAnalyzer,
        IImpactAnalyzer impactAnalyzer,
        IRiskEngine riskEngine,
        IRoslynCSharpAnalyzer roslynAnalyzer,
        IGitHubClientService gitHubClientService,
        ILogger<AnalyzerOrchestrator> logger)
    {
        _languageDetectionService = languageDetectionService;
        _complexityAnalyzer = complexityAnalyzer;
        _codeSmellDetector = codeSmellDetector;
        _dependencyAnalyzer = dependencyAnalyzer;
        _impactAnalyzer = impactAnalyzer;
        _riskEngine = riskEngine;
        _roslynAnalyzer = roslynAnalyzer;
        _gitHubClientService = gitHubClientService;
        _logger = logger;
    }

    public async Task<AnalysisResult> AnalyzeAsync(AnalysisContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Analyzing {RepoFullName} PR #{PullRequestNumber} ({FileCount} file(s) changed).",
            context.RepoFullName, context.PullRequestNumber, context.Files.Count);

        var languages = _languageDetectionService.Detect(context.Files);
        var complexity = _complexityAnalyzer.Analyze(context.Files);
        var codeSmells = _codeSmellDetector.Detect(context.Files);
        var dependencies = _dependencyAnalyzer.Analyze(context.Files);
        var impact = _impactAnalyzer.Analyze(context.Files);

        var roslynMetrics = await RunRoslynAnalysisAsync(context, codeSmells, cancellationToken);

        var risk = _riskEngine.CalculateRisk(complexity, codeSmells, dependencies, impact);
        var recommendations = _riskEngine.BuildRecommendations(complexity, codeSmells, dependencies, risk);

        var result = new AnalysisResult
        {
            RepoName = context.RepoFullName,
            PullRequestNumber = context.PullRequestNumber,
            BaseSha = context.BaseSha,
            HeadSha = context.HeadSha,
            Languages = languages,
            Complexity = complexity,
            Risk = risk,
            Dependencies = dependencies,
            CodeSmells = codeSmells,
            Impact = impact,
            Files = context.Files.Select(f => new FileChangeSummary
            {
                Path = f.Path,
                Status = f.Status,
                Additions = f.Additions,
                Deletions = f.Deletions
            }).ToList(),
            Recommendations = recommendations,
            RoslynMetrics = roslynMetrics
        };

        _logger.LogInformation(
            "Analysis complete for {RepoFullName} PR #{PullRequestNumber}: risk={Risk}, smells={SmellCount}.",
            context.RepoFullName, context.PullRequestNumber, risk, codeSmells.Count);

        return result;
    }
    private async Task<List<RoslynFileMetrics>> RunRoslynAnalysisAsync(
        AnalysisContext context, List<CodeSmell> codeSmells, CancellationToken cancellationToken)
    {
        var results = new List<RoslynFileMetrics>();

        var csharpFiles = context.Files
            .Where(f => f.Extension.Equals("cs", StringComparison.OrdinalIgnoreCase) && f.Status != ChangeStatus.Removed)
            .ToList();

        foreach (var file in csharpFiles)
        {
            var content = await _gitHubClientService.GetFileContentAsync(
                context.Owner, context.Repo, file.Path, context.HeadSha, cancellationToken);

            if (string.IsNullOrEmpty(content))
            {
                continue;
            }

            var metrics = _roslynAnalyzer.Analyze(file.Path, content);
            results.Add(metrics);

            codeSmells.AddRange(metrics.Findings.Select(finding => new CodeSmell
            {
                File = file.Path,
                Line = finding.Line,
                Type = finding.Type,
                Description = finding.Description,
                Severity = finding.Type switch
                {
                    "EmptyCatchBlock" => CodeSmellSeverity.High,
                    "DeepNesting" => CodeSmellSeverity.Medium,
                    _ => CodeSmellSeverity.Low
                }
            }));
        }

        return results;
    }
}
