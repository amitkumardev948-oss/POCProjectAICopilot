using Engineering_IntelligenceTools.Models.GitHub;

namespace Engineering_IntelligenceTools.Services.Interfaces;
public interface IGitHubClientService
{
    Task<IReadOnlyList<ChangedFile>> GetPullRequestFilesAsync(string owner, string repo, int pullRequestNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChangedFile>> CompareCommitsAsync(string owner, string repo, string baseSha, string headSha, CancellationToken cancellationToken = default);
    Task<string?> GetFileContentAsync(string owner, string repo, string path, string sha, CancellationToken cancellationToken = default);
    Task<(string BaseSha, string HeadSha)> GetPullRequestShaRangeAsync(string owner, string repo, int pullRequestNumber, CancellationToken cancellationToken = default);
}
