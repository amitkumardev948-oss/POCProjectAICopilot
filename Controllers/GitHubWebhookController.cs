using System.Text;
using System.Text.Json;
using Engineering_IntelligenceTools.Models.Analysis;
using Engineering_IntelligenceTools.Models.GitHub;
using Engineering_IntelligenceTools.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Engineering_IntelligenceTools.Controllers;


[ApiController]
[Route("api/webhooks/github")]
public class GitHubWebhookController : ControllerBase
{
    private readonly IWebhookSignatureValidator _signatureValidator;
    private readonly IGitHubClientService _gitHubClientService;
    private readonly IBackgroundAnalysisQueue _queue;
    private readonly ILogger<GitHubWebhookController> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public GitHubWebhookController(
        IWebhookSignatureValidator signatureValidator,
        IGitHubClientService gitHubClientService,
        IBackgroundAnalysisQueue queue,
        ILogger<GitHubWebhookController> logger)
    {
        _signatureValidator = signatureValidator;
        _gitHubClientService = gitHubClientService;
        _queue = queue;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        var rawBodyBytes = Encoding.UTF8.GetBytes(rawBody);

        var signature = Request.Headers["X-Hub-Signature-256"].ToString();
        if (!_signatureValidator.IsValid(signature, rawBodyBytes))
        {
            _logger.LogWarning("Rejected GitHub webhook with invalid signature.");
            return Unauthorized(new { error = "Invalid webhook signature." });
        }

        var eventType = Request.Headers["X-GitHub-Event"].ToString();

        switch (eventType)
        {
            case "pull_request":
                return await HandlePullRequestEventAsync(rawBody, cancellationToken);

            case "push":
                return await HandlePushEventAsync(rawBody, cancellationToken);

            case "ping":
                return Ok(new { message = "pong" });

            default:
                _logger.LogInformation("Ignoring unsupported GitHub event type: {EventType}", eventType);
                return Ok(new { message = $"Event '{eventType}' is not analyzed." });
        }
    }

    private async Task<IActionResult> HandlePullRequestEventAsync(string rawBody, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<PullRequestWebhookPayload>(rawBody, JsonOptions);
        if (payload is null)
        {
            return BadRequest(new { error = "Could not parse pull_request payload." });
        }

        if (!payload.ShouldAnalyze())
        {
            return Ok(new { message = $"Action '{payload.Action}' does not require analysis." });
        }

        var owner = payload.Repository.Owner.Login;
        var repo = payload.Repository.Name;

        var files = await _gitHubClientService.GetPullRequestFilesAsync(owner, repo, payload.Number, cancellationToken);

        var context = new AnalysisContext
        {
            Owner = owner,
            Repo = repo,
            PullRequestNumber = payload.Number,
            BaseSha = payload.PullRequest.Base.Sha,
            HeadSha = payload.PullRequest.Head.Sha,
            Files = files
        };

        _queue.Enqueue(context);

        _logger.LogInformation(
            "Queued analysis for {Owner}/{Repo} PR #{PullRequestNumber}.", owner, repo, payload.Number);

        return Accepted(new { message = "Analysis queued.", repo = context.RepoFullName, pullRequestNumber = payload.Number });
    }

    private async Task<IActionResult> HandlePushEventAsync(string rawBody, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<PushWebhookPayload>(rawBody, JsonOptions);
        if (payload is null)
        {
            return BadRequest(new { error = "Could not parse push payload." });
        }

        if (!payload.HasComparableRange)
        {
            return Ok(new { message = "Push has no comparable commit range (e.g. new branch)." });
        }

        var owner = payload.Repository.Owner.Login;
        var repo = payload.Repository.Name;

        var files = await _gitHubClientService.CompareCommitsAsync(
            owner, repo, payload.Before, payload.After, cancellationToken);

        var context = new AnalysisContext
        {
            Owner = owner,
            Repo = repo,
            PullRequestNumber = null,
            BaseSha = payload.Before,
            HeadSha = payload.After,
            Files = files
        };

        _queue.Enqueue(context);

        _logger.LogInformation("Queued analysis for {Owner}/{Repo} push {Before}..{After}.", owner, repo, payload.Before, payload.After);

        return Accepted(new { message = "Analysis queued.", repo = context.RepoFullName });
    }
}
