using System.Text.Json.Serialization;

namespace Engineering_IntelligenceTools.Models.GitHub;

/// <summary>
/// Subset of GitHub's "pull_request" webhook event payload that we actually need.
/// Full payload reference: https://docs.github.com/en/webhooks/webhook-events-and-payloads#pull_request
/// </summary>
public class PullRequestWebhookPayload
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("pull_request")]
    public PullRequestInfo PullRequest { get; set; } = new();

    [JsonPropertyName("repository")]
    public RepositoryInfo Repository { get; set; } = new();

    /// <summary>
    /// Actions that should trigger a (re)analysis. "synchronize" fires on every new push to the PR branch.
    /// </summary>
    public static readonly string[] AnalyzableActions = { "opened", "synchronize", "reopened" };

    public bool ShouldAnalyze() => AnalyzableActions.Contains(Action);
}

public class PullRequestInfo
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("base")]
    public CommitRef Base { get; set; } = new();

    [JsonPropertyName("head")]
    public CommitRef Head { get; set; } = new();
}

public class CommitRef
{
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;

    [JsonPropertyName("ref")]
    public string Ref { get; set; } = string.Empty;
}

public class RepositoryInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("owner")]
    public OwnerInfo Owner { get; set; } = new();
}

public class OwnerInfo
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
}
