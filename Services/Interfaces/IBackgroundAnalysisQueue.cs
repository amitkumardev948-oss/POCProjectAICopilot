using Engineering_IntelligenceTools.Models.Analysis;

namespace Engineering_IntelligenceTools.Services.Interfaces;

public interface IBackgroundAnalysisQueue
{
    void Enqueue(AnalysisContext context);
    Task<AnalysisContext> DequeueAsync(CancellationToken cancellationToken);
}
