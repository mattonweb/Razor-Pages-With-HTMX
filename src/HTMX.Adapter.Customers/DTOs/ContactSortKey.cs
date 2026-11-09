namespace HTMX.Adapter.Customers.DTOs;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The contact a cursor names, as an organization's list of contacts sorts them: their name, and their identifier,
/// which settles a tie. The next page starts just past these values.
/// </summary>
internal sealed record ContactSortKey
{
    /// <summary>
    /// Gets the contact's identifier.
    /// </summary>
    required public ContactId Id { get; init; }

    /// <summary>
    /// Gets the contact's name.
    /// </summary>
    required public DisplayName Name { get; init; }
}
