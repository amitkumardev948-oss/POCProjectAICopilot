using System.Text.Json.Serialization;
using Engineering_IntelligenceTools.Models.Enums;

namespace Engineering_IntelligenceTools.Models.Analysis;

public class CodeSmell
{
    public required string File { get; init; }
    public int? Line { get; init; }
    public required string Type { get; init; }
    public required string Description { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CodeSmellSeverity Severity { get; init; }
}
