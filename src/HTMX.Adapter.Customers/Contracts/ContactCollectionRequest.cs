namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Which page of one organization's contacts to read. The contacts are in name order, archived ones among the rest.
/// </summary>
public sealed record ContactCollectionRequest
{
    /// <summary>
    /// Gets the organization whose contacts to read.
    /// </summary>
    required public OrganizationId Organization { get; init; }

    /// <summary>
    /// Gets where the page lies, just after or just before one contact, or <see langword="null"/> for the first page.
    /// </summary>
    public ContactCursor? Cursor { get; init; }

    /// <summary>
    /// Gets how many contacts a page holds.
    /// </summary>
    required public PageSize PageSize { get; init; }
}
