namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// One page of an organization's contacts, with the count a pager shows.
/// </summary>
public sealed record ContactCollectionResponse
{
    /// <summary>
    /// Gets the request this page answers. When its cursor named a contact the organization no longer has, or left
    /// nothing to show, the cursor is gone: the page is the first page.
    /// </summary>
    required public ContactCollectionRequest Request { get; init; }

    /// <summary>
    /// Gets the contacts on this page, in name order.
    /// </summary>
    required public ContactSummary[] Items { get; init; }

    /// <summary>
    /// Gets where the page before this one lies, or <see langword="null"/> on the first page.
    /// </summary>
    public ContactCursor? Previous { get; init; }

    /// <summary>
    /// Gets where the page after this one lies, or <see langword="null"/> on the last page.
    /// </summary>
    public ContactCursor? Next { get; init; }

    /// <summary>
    /// Gets how many contacts the organization has, archived ones included, on every page together.
    /// </summary>
    required public RecordCount Total { get; init; }
}
