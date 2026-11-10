namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// One page of the users list, with the counts a status filter and a pager show.
/// </summary>
public sealed record UserCollectionResponse
{
    /// <summary>
    /// Gets the request this page answers. When its cursor named a user who no longer exists, or left nothing to show,
    /// the cursor is gone: the page is the first page.
    /// </summary>
    required public UserCollectionRequest Request { get; init; }

    /// <summary>
    /// Gets the users on this page, in order.
    /// </summary>
    required public UserSummary[] Items { get; init; }

    /// <summary>
    /// Gets where the page before this one lies, or <see langword="null"/> on the first page.
    /// </summary>
    public UserCursor? Previous { get; init; }

    /// <summary>
    /// Gets where the page after this one lies, or <see langword="null"/> on the last page.
    /// </summary>
    public UserCursor? Next { get; init; }

    /// <summary>
    /// Gets how many users match the filter and the status, on every page together.
    /// </summary>
    required public RecordCount Matching { get; init; }

    /// <summary>
    /// Gets how many active users match the filter.
    /// </summary>
    required public RecordCount Active { get; init; }

    /// <summary>
    /// Gets how many archived users match the filter.
    /// </summary>
    required public RecordCount Archived { get; init; }

    /// <summary>
    /// Gets how many users match the filter, active and archived together.
    /// </summary>
    required public RecordCount All { get; init; }
}
