using Engineering_IntelligenceTools.Models.Analysis;
using Engineering_IntelligenceTools.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Engineering_IntelligenceTools.Controllers;

[ApiController]
[Route("api/analysis")]
public class AnalysisController : ControllerBase
{
    private readonly IGitHubClientService _gitHubClientService;
    private readonly IAnalyzerOrchestrator _orchestrator;
    private readonly IAnalysisResultStore _resultStore;
    private readonly ILogger<AnalysisController> _logger;
    //Amit changes
    public AnalysisController(
            IGitHubClientService gitHubClientService,
        IAnalyzerOrchestrator orchestrator,
        IAnalysisResultStore resultStore,
        ILogger<AnalysisController> logger)
    {
        _gitHubClientService = gitHubClientService;
        _orchestrator = orchestrator;
        _resultStore = resultStore;
        _logger = logger;
    }
    [HttpPost("run")]
    [ProducesResponseType(typeof(AnalysisResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Run([FromBody] AnalysisRequest request, CancellationToken cancellationToken)
    {
        if (request.PullRequestNumber is null && (request.BaseSha is null || request.HeadSha is null))
        {
             return BadRequest(new
            {
                error = "Provide either pullRequestNumber, or both baseSha and headSha."
            });
        }

        try
        {
            AnalysisContext context;

            if (request.PullRequestNumber is { } prNumber)
            {
                   var files = await _gitHubClientService.GetPullRequestFilesAsync(
                    request.Owner, request.Repo, prNumber, cancellationToken);

                var (baseSha, headSha) = await _gitHubClientService.GetPullRequestShaRangeAsync(
                    request.Owner, request.Repo, prNumber, cancellationToken);

                context = new AnalysisContext
                {
                    Owner = request.Owner,
                    Repo = request.Repo,
                    PullRequestNumber = prNumber,
                    BaseSha = baseSha,
                    HeadSha = headSha,
                    Files = files
                };
            }
            else
            {
                var files = await _gitHubClientService.CompareCommitsAsync(
                    request.Owner, request.Repo, request.BaseSha!, request.HeadSha!, cancellationToken);

                context = new AnalysisContext
                {
                    Owner = request.Owner,
                    Repo = request.Repo,
                    PullRequestNumber = null,
                    BaseSha = request.BaseSha!,
                    HeadSha = request.HeadSha!,
                    Files = files
                };
            }

            var result = await _orchestrator.AnalyzeAsync(context, cancellationToken);
            _resultStore.Save(result);

            return Ok(result);
        }
        catch (Octokit.NotFoundException)
        {
            return NotFound(new { error = $"Repository or PR not found: {request.Owner}/{request.Repo}" });
        }
        catch (Octokit.ForbiddenException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "GitHub API access forbidden - check the configured access token/permissions."
            });
        }
    }

    [HttpGet("{owner}/{repo}")]
    [ProducesResponseType(typeof(AnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetLatest(string owner, string repo, [FromQuery] int? pullRequestNumber)
    {
        var result = _resultStore.Get($"{owner}/{repo}", pullRequestNumber);
        return result is null
            ? NotFound(new { error = "No analysis found for this repo/PR yet." })
            : Ok(result);
    }
}
