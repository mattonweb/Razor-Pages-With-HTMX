namespace HTMX.Application.Web.Pages;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// One version of a record as its History tab shows it: what happened, who did it and when, what it changed, and what
/// rolling back to it would change now.
/// </summary>
public sealed record TimelineEntry
{
    /// <summary>
    /// Gets the version's number.
    /// </summary>
    required public VersionNumber Version { get; init; }

    /// <summary>
    /// Gets what happened.
    /// </summary>
    required public ChangeKind Change { get; init; }

    /// <summary>
    /// Gets the version a roll-back went back to, or <see langword="null"/> for any other change.
    /// </summary>
    public VersionNumber? RolledBackTo { get; init; }

    /// <summary>
    /// Gets when it happened, and who did it.
    /// </summary>
    required public RecordChange Changed { get; init; }

    /// <summary>
    /// Gets the fields the version changed from the one before it; none for the first version.
    /// </summary>
    required public FieldChange[] Changes { get; init; }

    /// <summary>
    /// Gets the fields rolling back to the version would change now; none when it is the current version, when the
    /// record cannot be rolled back, or when nothing would change.
    /// </summary>
    required public FieldChange[] RollBack { get; init; }

    /// <summary>
    /// Gets what the entry says in place of a roll-back, such as that the version holds the same values as now, or
    /// <see langword="null"/> when it says nothing.
    /// </summary>
    public string? Note { get; init; }
}
