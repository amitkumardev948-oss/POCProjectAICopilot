using System.Threading.Channels;
using Engineering_IntelligenceTools.Models.Analysis;
using Engineering_IntelligenceTools.Services.Interfaces;

namespace Engineering_IntelligenceTools.Services;

public class BackgroundAnalysisQueue : IBackgroundAnalysisQueue
{
    private readonly Channel<AnalysisContext> _channel = Channel.CreateUnbounded<AnalysisContext>();

    public void Enqueue(AnalysisContext context)
    {
        if (!_channel.Writer.TryWrite(context))
        {
            throw new InvalidOperationException("Failed to enqueue analysis context.");
        }
    }

    public async Task<AnalysisContext> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
