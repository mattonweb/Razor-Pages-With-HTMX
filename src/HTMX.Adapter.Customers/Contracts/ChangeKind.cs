namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What a version records happening to its record. Every change makes a version, so each kind is one entry in the
/// History tab.
/// </summary>
public enum ChangeKind
{
    /// <summary>
    /// The record was created; its first version holds every value as it was entered.
    /// </summary>
    Created,

    /// <summary>
    /// One or more values were changed and saved.
    /// </summary>
    Edited,

    /// <summary>
    /// The record was archived: hidden from the Active list and closed to edits until it is restored.
    /// </summary>
    Archived,

    /// <summary>
    /// An archived record was made active again.
    /// </summary>
    Restored,

    /// <summary>
    /// An earlier version's values were saved again as a new version.
    /// </summary>
    RolledBack,
}
