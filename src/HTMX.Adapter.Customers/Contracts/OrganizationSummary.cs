namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// One organization as a list shows it: enough to recognize it and open it, not every value it holds.
/// </summary>
public sealed record OrganizationSummary
{
    /// <summary>
    /// Gets the organization's identifier.
    /// </summary>
    required public OrganizationId Id { get; init; }

    /// <summary>
    /// Gets the organization's name.
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
    /// Gets how many contacts belong to the organization, archived ones included.
    /// </summary>
    required public RecordCount Contacts { get; init; }

    /// <summary>
    /// Gets when the organization was last changed, in UTC.
    /// </summary>
    required public DateTime LastModifiedOn { get; init; }

    /// <summary>
    /// Gets when the organization was archived, in UTC, or <see langword="null"/> while it is active.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }
}
