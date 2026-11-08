namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What a list of organizations can be sorted by.
/// </summary>
public enum OrganizationSortColumn
{
    /// <summary>
    /// The organization's name, however it is capitalized.
    /// </summary>
    Name,

    /// <summary>
    /// The organization's industry.
    /// </summary>
    Industry,

    /// <summary>
    /// How many contacts belong to the organization.
    /// </summary>
    Contacts,

    /// <summary>
    /// When the organization was last changed.
    /// </summary>
    Updated,
}
