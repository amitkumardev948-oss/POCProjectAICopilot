using Engineering_IntelligenceTools.Services.Interfaces;

namespace Engineering_IntelligenceTools.Services;
public class BackgroundAnalysisWorker : BackgroundService
{
    private readonly IBackgroundAnalysisQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundAnalysisWorker> _logger;

    public BackgroundAnalysisWorker(
        IBackgroundAnalysisQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundAnalysisWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            AnalysisContextWrapper? contextWrapper = null;
            try
            {
                var context = await _queue.DequeueAsync(stoppingToken);
                contextWrapper = new AnalysisContextWrapper(context);

                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IAnalyzerOrchestrator>();
                var store = scope.ServiceProvider.GetRequiredService<IAnalysisResultStore>();

                var result = await orchestrator.AnalyzeAsync(context, stoppingToken);
                store.Save(result);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process queued analysis job for {RepoFullName}.",
                    contextWrapper?.Context.RepoFullName ?? "unknown");
            }
        }
    }

    private sealed record AnalysisContextWrapper(Models.Analysis.AnalysisContext Context);
}
