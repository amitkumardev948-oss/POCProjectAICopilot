using Engineering_IntelligenceTools.Configuration;
using Engineering_IntelligenceTools.Services;
using Engineering_IntelligenceTools.Services.Interfaces;
using Engineering_IntelligenceTools.Services.Roslyn;

namespace Engineering_IntelligenceTools.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEngineeringIntelligenceServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GitHubOptions>(configuration.GetSection(GitHubOptions.SectionName));

        services.AddScoped<IWebhookSignatureValidator, WebhookSignatureValidator>();
        services.AddScoped<IGitHubClientService, GitHubClientService>();

        services.AddScoped<ILanguageDetectionService, LanguageDetectionService>();
        services.AddScoped<IComplexityAnalyzer, ComplexityAnalyzer>();
        services.AddScoped<ICodeSmellDetector, CodeSmellDetector>();
        services.AddScoped<IDependencyAnalyzer, DependencyAnalyzer>();
        services.AddScoped<IImpactAnalyzer, ImpactAnalyzer>();
        services.AddScoped<IRiskEngine, RiskEngine>();
        services.AddScoped<IRoslynCSharpAnalyzer, RoslynCSharpAnalyzer>();
        services.AddScoped<IAnalyzerOrchestrator, AnalyzerOrchestrator>();

        services.AddSingleton<IAnalysisResultStore, InMemoryAnalysisResultStore>();
        services.AddSingleton<IBackgroundAnalysisQueue, BackgroundAnalysisQueue>();
        services.AddHostedService<BackgroundAnalysisWorker>();

        return services;
    }
}
