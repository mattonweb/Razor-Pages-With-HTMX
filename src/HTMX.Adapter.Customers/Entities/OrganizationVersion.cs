namespace HTMX.Adapter.Customers.Entities;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// One version of an organization: what happened, who did it and when, and a copy of the organization's values as
/// that change left them.
/// </summary>
/// <remarks>
/// Versions are only ever added. The History tab compares each version with the one before it to show what changed,
/// and a roll-back saves an older version's values as a new version, so history is never rewritten.
/// </remarks>
internal sealed record OrganizationVersion
{
    /// <summary>
    /// Gets the organization this is a version of.
    /// </summary>
    required public OrganizationId OrganizationId { get; init; }

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
    /// Gets when it happened.
    /// </summary>
    required public DateTime ChangedOn { get; init; }

    /// <summary>
    /// Gets who did it.
    /// </summary>
    required public UserId ChangedBy { get; init; }

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

    /// <summary>
    /// Gets who archived the organization, if this version left it archived.
    /// </summary>
    public UserId? ArchivedBy { get; init; }
}
