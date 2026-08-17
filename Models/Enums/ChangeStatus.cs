namespace Engineering_IntelligenceTools.Models.Enums;

/// <summary>
/// Mirrors the "status" field GitHub returns for a changed file
/// (added, modified, removed, renamed, copied, changed, unchanged).
/// </summary>
public enum ChangeStatus
{
    Added,
    Modified,
    Removed,
    Renamed,
    Copied,
    Unknown
}
