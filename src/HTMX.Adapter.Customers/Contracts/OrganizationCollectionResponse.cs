namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// One page of the organizations list, with the counts a status filter and a pager show.
/// </summary>
public sealed record OrganizationCollectionResponse
{
    /// <summary>
    /// Gets the request this page answers. When its cursor named an organization that no longer exists, or left
    /// nothing to show, the cursor is gone: the page is the first page.
    /// </summary>
    required public OrganizationCollectionRequest Request { get; init; }

    /// <summary>
    /// Gets the organizations on this page, in order.
    /// </summary>
    required public OrganizationSummary[] Items { get; init; }

    /// <summary>
    /// Gets where the page before this one lies, or <see langword="null"/> on the first page.
    /// </summary>
    public OrganizationCursor? Previous { get; init; }

    /// <summary>
    /// Gets where the page after this one lies, or <see langword="null"/> on the last page.
    /// </summary>
    public OrganizationCursor? Next { get; init; }

    /// <summary>
    /// Gets how many organizations match the filter and the status, on every page together.
    /// </summary>
    required public RecordCount Matching { get; init; }

    /// <summary>
    /// Gets how many active organizations match the filter.
    /// </summary>
    required public RecordCount Active { get; init; }

    /// <summary>
    /// Gets how many archived organizations match the filter.
    /// </summary>
    required public RecordCount Archived { get; init; }

    /// <summary>
    /// Gets how many organizations match the filter, active and archived together.
    /// </summary>
    required public RecordCount All { get; init; }
}
