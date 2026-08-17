using Engineering_IntelligenceTools.Models.Analysis;
using Engineering_IntelligenceTools.Models.Enums;
using Engineering_IntelligenceTools.Services.Interfaces;

namespace Engineering_IntelligenceTools.Services;
public class RiskEngine : IRiskEngine
{
    public RiskLevel CalculateRisk(
        ComplexityMetrics complexity,
        List<CodeSmell> codeSmells,
        DependencyChanges dependencies,
        ImpactSummary impact)
    {
        double points = 0;

        points += complexity.Score;
        points += codeSmells.Count(s => s.Severity == CodeSmellSeverity.High) * 2.5;
        points += codeSmells.Count(s => s.Severity == CodeSmellSeverity.Medium) * 1.5;
        points += codeSmells.Count(s => s.Severity == CodeSmellSeverity.Low) * 0.5;
        points += (dependencies.Added.Count + dependencies.Removed.Count) * 0.75;
        points += impact.CriticalPathTouched ? 3.0 : 0;

        return points switch
        {
            >= 14 => RiskLevel.Critical,
            >= 9 => RiskLevel.High,
            >= 4 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
    }

    public List<string> BuildRecommendations(
        ComplexityMetrics complexity,
        List<CodeSmell> codeSmells,
        DependencyChanges dependencies,
        RiskLevel risk)
    {
        var recommendations = new List<string>();

        if (codeSmells.Any(s => s.Type == "UnguardedRowAccess"))
        {
            recommendations.Add(
                "Add a null/row-count check before indexing into DataSet/DataTable rows.");
        }

        if (codeSmells.Any(s => s.Type == "EmptyCatchBlock"))
        {
            recommendations.Add(
                "Avoid empty catch blocks - log the exception or rethrow, don't swallow it silently.");
        }

        if (codeSmells.Any(s => s.Type == "HardcodedSecret"))
        {
            recommendations.Add(
                "Move hardcoded credentials/secrets into configuration or a secrets manager.");
        }

        if (codeSmells.Any(s => s.Type == "ConsoleDebugLeftIn"))
        {
            recommendations.Add(
                "Remove leftover debug print statements before merging.");
        }

        if (complexity.MaxNestingDepthDelta >= 4)
        {
            recommendations.Add(
                "Deeply nested logic detected - consider extracting guard clauses or smaller methods.");
        }

        if (dependencies.Added.Count > 0)
        {
            recommendations.Add(
                $"Review {dependencies.Added.Count} newly added dependenc{(dependencies.Added.Count == 1 ? "y" : "ies")} for license/security impact.");
        }

        if (risk is RiskLevel.High or RiskLevel.Critical)
        {
            recommendations.Add(
                "Risk is elevated - recommend an additional reviewer before merging.");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("No significant issues detected by static analysis.");
        }

        return recommendations;
    }
}
