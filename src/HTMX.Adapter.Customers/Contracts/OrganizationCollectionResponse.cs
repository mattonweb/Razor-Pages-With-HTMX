namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// One page of the organizations list, with the counts a status filter and a pager show.
/// </summary>
public sealed record OrganizationCollectionResponse
{
    /// <summary>
    /// Gets the request this page answers, with its page number moved back to the last page when it asked for one
    /// past the end.
    /// </summary>
    required public OrganizationCollectionRequest Request { get; init; }

    /// <summary>
    /// Gets the organizations on this page, in order.
    /// </summary>
    required public OrganizationSummary[] Items { get; init; }

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
