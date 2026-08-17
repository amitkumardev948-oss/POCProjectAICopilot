namespace Engineering_IntelligenceTools.Models.Analysis;

public class DependencyChange
{
    public required string Name { get; init; }
    public string? Version { get; init; }
    public required string ManifestFile { get; init; }
}

public class DependencyChanges
{
    public List<DependencyChange> Added { get; init; } = new();
    public List<DependencyChange> Removed { get; init; } = new();
}
