using Engineering_IntelligenceTools.Models.Enums;

namespace Engineering_IntelligenceTools.Models.GitHub;

/// <summary>
/// A single file changed between two commits (PR base..head, or push before..after).
/// This is the normalized shape the rest of the pipeline works with, regardless of
/// whether the data came from the "compare commits" API or the "list PR files" API.
/// </summary>
public class ChangedFile
{
    public required string Path { get; init; }
    public string? PreviousPath { get; init; }
    public ChangeStatus Status { get; init; }
    public int Additions { get; init; }
    public int Deletions { get; init; }

    /// <summary>
    /// Unified diff text for this file (GitHub's "patch" field).
    /// Can be null for binary files or when GitHub omits large diffs.
    /// </summary>
    public string? Patch { get; init; }

    public int TotalChanges => Additions + Deletions;

    public string Extension =>
        System.IO.Path.GetExtension(Path).TrimStart('.').ToLowerInvariant();
}
