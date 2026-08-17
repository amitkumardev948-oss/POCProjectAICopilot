using Engineering_IntelligenceTools.Configuration;
using Engineering_IntelligenceTools.Models.Enums;
using Engineering_IntelligenceTools.Models.GitHub;
using Engineering_IntelligenceTools.Services.Interfaces;
using Microsoft.Extensions.Options;
using Octokit;

namespace Engineering_IntelligenceTools.Services;

public class GitHubClientService : IGitHubClientService
{
    private readonly GitHubClient _client;
    private readonly ILogger<GitHubClientService> _logger;

    public GitHubClientService(IOptions<GitHubOptions> options, ILogger<GitHubClientService> logger)
    {
        _logger = logger;
        var settings = options.Value;

        _client = new GitHubClient(new ProductHeaderValue(settings.ProductName));

        if (!string.IsNullOrWhiteSpace(settings.AccessToken))
        {
            _client.Credentials = new Credentials(settings.AccessToken);
        }
        else
        {
            _logger.LogWarning(
                "GitHub:AccessToken is not configured - requests will be unauthenticated and heavily rate limited.");
        }
    }

    public async Task<IReadOnlyList<ChangedFile>> GetPullRequestFilesAsync(
        string owner, string repo, int pullRequestNumber, CancellationToken cancellationToken = default)
    {
        var files = await _client.PullRequest.Files(owner, repo, pullRequestNumber);
        return files.Select(MapPullRequestFile).ToList();
    }

    public async Task<IReadOnlyList<ChangedFile>> CompareCommitsAsync(
        string owner, string repo, string baseSha, string headSha, CancellationToken cancellationToken = default)
    {
        var comparison = await _client.Repository.Commit.Compare(owner, repo, baseSha, headSha);
        return comparison.Files.Select(MapPullRequestFile).ToList();
    }

    public async Task<string?> GetFileContentAsync(
        string owner, string repo, string path, string sha, CancellationToken cancellationToken = default)
    {
        try
        {
            var contents = await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, sha);
            return contents.FirstOrDefault()?.Content;
        }
        catch (NotFoundException)
        {
            // File was deleted, or path is a directory - both are expected, non-fatal cases.
            return null;
        }
    }

    public async Task<(string BaseSha, string HeadSha)> GetPullRequestShaRangeAsync(
        string owner, string repo, int pullRequestNumber, CancellationToken cancellationToken = default)
    {
        var pullRequest = await _client.PullRequest.Get(owner, repo, pullRequestNumber);
        return (pullRequest.Base.Sha, pullRequest.Head.Sha);
    }

    private static ChangedFile MapPullRequestFile(PullRequestFile file) => new()
    {
        Path = file.FileName,
        PreviousPath = file.PreviousFileName,
        Status = MapStatus(file.Status),
        Additions = file.Additions,
        Deletions = file.Deletions,
        Patch = file.Patch
    };

    private static ChangedFile MapPullRequestFile(GitHubCommitFile file) => new()
    {
        Path = file.Filename,
        PreviousPath = file.PreviousFileName,
        Status = MapStatus(file.Status),
        Additions = file.Additions,
        Deletions = file.Deletions,
        Patch = file.Patch
    };

    private static ChangeStatus MapStatus(string status) => status switch
    {
        "added" => ChangeStatus.Added,
        "modified" or "changed" => ChangeStatus.Modified,
        "removed" => ChangeStatus.Removed,
        "renamed" => ChangeStatus.Renamed,
        "copied" => ChangeStatus.Copied,
        _ => ChangeStatus.Unknown
    };
}
