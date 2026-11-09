namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// An organization as another record names it, such as the organization a contact belongs to.
/// </summary>
public sealed record OrganizationReference
{
    /// <summary>
    /// Gets the organization's identifier.
    /// </summary>
    required public OrganizationId Id { get; init; }

    /// <summary>
    /// Gets the organization's name, as it is now.
    /// </summary>
    required public OrganizationName Name { get; init; }

    /// <summary>
    /// Gets when the organization was archived, in UTC, or <see langword="null"/> while it is active.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }
}
