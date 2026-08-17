namespace Engineering_IntelligenceTools.Configuration;

public class GitHubOptions
{
    public const string SectionName = "GitHub";
    public string AccessToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string ProductName { get; set; } = "EngineeringIntelligenceTools";
    public string[] CriticalPathMarkers { get; set; } =
    {
        "auth", "security", "payment", "billing", "identity", "token"
    };
}
