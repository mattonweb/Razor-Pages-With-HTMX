namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Which page of the users list to read: filtered, narrowed by status, and sorted.
/// </summary>
public sealed record UserCollectionRequest
{
    /// <summary>
    /// Gets the text the name, the username or the email address must contain, or <see langword="null"/> for every
    /// user.
    /// </summary>
    public FilterText? Filter { get; init; }

    /// <summary>
    /// Gets which users to show by whether they are archived.
    /// </summary>
    required public RecordStatus Status { get; init; }

    /// <summary>
    /// Gets what to sort by. Users who tie are in identifier order.
    /// </summary>
    required public UserSortColumn Sort { get; init; }

    /// <summary>
    /// Gets which way to sort.
    /// </summary>
    required public SortDirection Direction { get; init; }

    /// <summary>
    /// Gets where the page lies, just after or just before one user, or <see langword="null"/> for the first page.
    /// </summary>
    public UserCursor? Cursor { get; init; }

    /// <summary>
    /// Gets how many users a page holds.
    /// </summary>
    required public PageSize PageSize { get; init; }
}
