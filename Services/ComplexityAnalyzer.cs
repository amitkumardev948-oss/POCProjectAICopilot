using Engineering_IntelligenceTools.Models.Analysis;
using Engineering_IntelligenceTools.Models.GitHub;
using Engineering_IntelligenceTools.Services.Interfaces;

namespace Engineering_IntelligenceTools.Services;
public class ComplexityAnalyzer : IComplexityAnalyzer
{
    public ComplexityMetrics Analyze(IReadOnlyList<ChangedFile> files)
    {
        if (files.Count == 0)
        {
            return new ComplexityMetrics();
        }

        var totalLinesChanged = files.Sum(f => f.TotalChanges);
        var maxNestingDelta = files.Max(EstimateNestingDepth);
        var averageChangeSize = (double)totalLinesChanged / files.Count;

        var score = ScoreFrom(totalLinesChanged, maxNestingDelta, files.Count);

        return new ComplexityMetrics
        {
            Score = Math.Round(score, 1),
            TotalLinesChanged = totalLinesChanged,
            FilesChanged = files.Count,
            MaxNestingDepthDelta = maxNestingDelta,
            AverageChangeSizePerFile = Math.Round(averageChangeSize, 1)
        };
    }
    private static int EstimateNestingDepth(ChangedFile file)
    {
        if (string.IsNullOrEmpty(file.Patch))
        {
            return 0;
        }

        var depth = 0;
        var maxDepth = 0;

        foreach (var line in file.Patch.Split('\n'))
        {
            if (!line.StartsWith('+') || line.StartsWith("+++"))
            {
                continue;
            }

            foreach (var ch in line)
            {
                if (ch == '{') depth++;
                if (ch == '}') depth = Math.Max(0, depth - 1);
                maxDepth = Math.Max(maxDepth, depth);
            }
        }

        return maxDepth;
    }

    private static double ScoreFrom(int totalLinesChanged, int maxNestingDelta, int fileCount)
    {
        // Weighted, capped 0-10 score. Weights are intentionally simple/explainable
        // for Phase 1 - tune once real PR history is available.
        var linesComponent = Math.Min(totalLinesChanged / 50.0, 5.0);
        var nestingComponent = Math.Min(maxNestingDelta * 0.75, 3.0);
        var spreadComponent = Math.Min(fileCount / 5.0, 2.0);

        return Math.Min(linesComponent + nestingComponent + spreadComponent, 10.0);
    }
}
