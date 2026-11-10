namespace HTMX.Adapter.Customers.Entities;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The organization a cursor names, as the list sorts it: its value in every sortable column, and its identifier,
/// which settles a tie. The next page starts just past these values, whichever column the list is sorted by.
/// </summary>
/// <remarks>
/// Unlike the other entities, it has no table: a query reads it from the organization's row and counts its contacts,
/// and nothing saves it.
/// </remarks>
internal sealed record OrganizationSortKey
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
    /// Gets how many contacts belong to the organization.
    /// </summary>
    required public RecordCount Contacts { get; init; }

    /// <summary>
    /// Gets when the organization was last changed, in UTC.
    /// </summary>
    required public DateTime LastModifiedOn { get; init; }
}
