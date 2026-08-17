using Engineering_IntelligenceTools.Models.Analysis;

namespace Engineering_IntelligenceTools.Services.Interfaces;
public interface IRoslynCSharpAnalyzer
{
    RoslynFileMetrics Analyze(string filePath, string sourceText);
}
