using System.Text.Json.Serialization;
using Engineering_IntelligenceTools.Models.Enums;

namespace Engineering_IntelligenceTools.Models.Analysis;

public class FileChangeSummary
{
    public required string Path { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChangeStatus Status { get; init; }

    public int Additions { get; init; }
    public int Deletions { get; init; }
}
