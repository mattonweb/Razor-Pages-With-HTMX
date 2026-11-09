namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// One version of an organization, as its History tab shows it: what happened, who did it and when, and the
/// organization's values as that change left them.
/// </summary>
public sealed record OrganizationVersionResponse
{
    /// <summary>
    /// Gets the version's number, counting from 1.
    /// </summary>
    required public VersionNumber Version { get; init; }

    /// <summary>
    /// Gets what happened.
    /// </summary>
    required public ChangeKind Change { get; init; }

    /// <summary>
    /// Gets the version whose values a roll-back saved again, or <see langword="null"/> for any other change.
    /// </summary>
    public VersionNumber? RolledBackTo { get; init; }

    /// <summary>
    /// Gets when it happened, and who did it.
    /// </summary>
    required public RecordChange Changed { get; init; }

    /// <summary>
    /// Gets the organization's name as this version left it.
    /// </summary>
    required public OrganizationName Name { get; init; }

    /// <summary>
    /// Gets the organization's industry as this version left it.
    /// </summary>
    required public Industry Industry { get; init; }

    /// <summary>
    /// Gets the organization's website as this version left it, if it had one.
    /// </summary>
    public WebAddress? Website { get; init; }

    /// <summary>
    /// Gets when the organization was archived, if this version left it archived.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }
}
