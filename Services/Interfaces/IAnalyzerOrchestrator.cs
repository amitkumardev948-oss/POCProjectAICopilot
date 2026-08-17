using Engineering_IntelligenceTools.Models.Analysis;

namespace Engineering_IntelligenceTools.Services.Interfaces;
public interface IAnalyzerOrchestrator
{
    Task<AnalysisResult> AnalyzeAsync(AnalysisContext context, CancellationToken cancellationToken = default);
}
