using Engineering_IntelligenceTools.Models.GitHub;

namespace Engineering_IntelligenceTools.Services.Interfaces;
public interface ILanguageDetectionService
{
    List<string> Detect(IReadOnlyList<ChangedFile> files);
}
