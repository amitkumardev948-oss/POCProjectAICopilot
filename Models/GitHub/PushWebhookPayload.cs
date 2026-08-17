using System.Text.Json.Serialization;

namespace Engineering_IntelligenceTools.Models.GitHub;

/// <summary>
/// Subset of GitHub's "push" webhook event payload that we actually need.
/// Full payload reference: https://docs.github.com/en/webhooks/webhook-events-and-payloads#push
/// </summary>
public class PushWebhookPayload
{
    [JsonPropertyName("ref")]
    public string Ref { get; set; } = string.Empty;

    [JsonPropertyName("before")]
    public string Before { get; set; } = string.Empty;

    [JsonPropertyName("after")]
    public string After { get; set; } = string.Empty;

    [JsonPropertyName("repository")]
    public RepositoryInfo Repository { get; set; } = new();

    /// <summary>
    /// "before" is all zeros on branch-creation pushes - nothing to diff against yet.
    /// </summary>
    public bool HasComparableRange =>
        !string.IsNullOrEmpty(Before) &&
        !Before.All(c => c == '0') &&
        !string.IsNullOrEmpty(After);
}
