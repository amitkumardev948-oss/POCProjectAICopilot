using Engineering_IntelligenceTools.Models.Analysis;
using Engineering_IntelligenceTools.Models.Enums;
using Engineering_IntelligenceTools.Models.GitHub;

namespace Engineering_IntelligenceTools.Services.Interfaces;

public interface IRiskEngine
{
    RiskLevel CalculateRisk(
        ComplexityMetrics complexity,
        List<CodeSmell> codeSmells,
        DependencyChanges dependencies,
        ImpactSummary impact);

    List<string> BuildRecommendations(
        ComplexityMetrics complexity,
        List<CodeSmell> codeSmells,
        DependencyChanges dependencies,
        RiskLevel risk);
}
