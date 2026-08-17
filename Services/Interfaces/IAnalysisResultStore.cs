using Engineering_IntelligenceTools.Models.Analysis;

namespace Engineering_IntelligenceTools.Services.Interfaces;
public interface IAnalysisResultStore
{
    void Save(AnalysisResult result);
    AnalysisResult? Get(string repoFullName, int? pullRequestNumber);
}
