namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Which page of the organizations list to read: filtered, narrowed by status, and sorted.
/// </summary>
public sealed record OrganizationCollectionRequest
{
    /// <summary>
    /// Gets the text the names must contain, or <see langword="null"/> for every organization.
    /// </summary>
    public FilterText? Filter { get; init; }

    /// <summary>
    /// Gets which organizations to show by whether they are archived.
    /// </summary>
    required public RecordStatus Status { get; init; }

    /// <summary>
    /// Gets what to sort by. Organizations that tie are in identifier order.
    /// </summary>
    required public OrganizationSortColumn Sort { get; init; }

    /// <summary>
    /// Gets which way to sort.
    /// </summary>
    required public SortDirection Direction { get; init; }

    /// <summary>
    /// Gets where the page lies, just after or just before one organization, or <see langword="null"/> for the first
    /// page.
    /// </summary>
    public OrganizationCursor? Cursor { get; init; }

    /// <summary>
    /// Gets how many organizations a page holds.
    /// </summary>
    required public PageSize PageSize { get; init; }
}
