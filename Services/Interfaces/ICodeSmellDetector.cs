using Engineering_IntelligenceTools.Models.Analysis;
using Engineering_IntelligenceTools.Models.GitHub;

namespace Engineering_IntelligenceTools.Services.Interfaces;

public interface ICodeSmellDetector
{
    List<CodeSmell> Detect(IReadOnlyList<ChangedFile> files);
}
