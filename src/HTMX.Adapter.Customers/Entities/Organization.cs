namespace HTMX.Adapter.Customers.Entities;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// An organization that contacts belong to, as the Organizations table holds it: its current values, who created it,
/// who changed it last and, once archived, who archived it.
/// </summary>
/// <remarks>
/// Every change to an organization also writes an <see cref="OrganizationVersion"/>, so the History tab can show what
/// changed and roll it back. An organization is active while <see cref="ArchivedOn"/> is not set. Every date is in UTC.
/// </remarks>
internal sealed record Organization
{
    /// <summary>
    /// Gets the identifier, which the database assigns when the organization is first saved.
    /// </summary>
    public OrganizationId Id { get; init; }

    /// <summary>
    /// Gets the name, unique however it is capitalized.
    /// </summary>
    required public OrganizationName Name { get; init; }

    /// <summary>
    /// Gets the industry the organization works in.
    /// </summary>
    required public Industry Industry { get; init; }

    /// <summary>
    /// Gets the organization's website, or <see langword="null"/> when it has none on record.
    /// </summary>
    public WebAddress? Website { get; init; }

    /// <summary>
    /// Gets when the organization was created.
    /// </summary>
    required public DateTime CreatedOn { get; init; }

    /// <summary>
    /// Gets who created the organization.
    /// </summary>
    required public UserId CreatedBy { get; init; }

    /// <summary>
    /// Gets when the organization was last changed, which is when it was created until its first change.
    /// </summary>
    required public DateTime LastModifiedOn { get; init; }

    /// <summary>
    /// Gets who changed the organization last.
    /// </summary>
    required public UserId LastModifiedBy { get; init; }

    /// <summary>
    /// Gets when the organization was archived, or <see langword="null"/> while it is active.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }

    /// <summary>
    /// Gets who archived the organization, or <see langword="null"/> while it is active.
    /// </summary>
    public UserId? ArchivedBy { get; init; }

    /// <summary>
    /// Gets the number of the organization's current version. A save that started from an older number is refused.
    /// </summary>
    required public VersionNumber Version { get; init; }
}
