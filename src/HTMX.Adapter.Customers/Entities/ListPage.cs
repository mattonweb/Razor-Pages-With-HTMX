namespace HTMX.Adapter.Customers.Entities;

/// <summary>
/// One page of a list, cut from the records read for it: the records it shows, in the list's order, and the cursors
/// for the pages either side of it, where there are any. Each list makes its own response from it.
/// </summary>
/// <remarks>
/// Like the sort keys, it has no table: <see cref="Services.ListPages"/> makes it from what a query read.
/// </remarks>
/// <typeparam name="TItem">The kind of record the list shows, such as an organization's summary.</typeparam>
/// <typeparam name="TCursor">The cursor that names one of those records, such as an organization's.</typeparam>
internal sealed record ListPage<TItem, TCursor>
    where TCursor : struct
{
    /// <summary>
    /// Gets the records the page shows, in the list's order.
    /// </summary>
    required public TItem[] Items { get; init; }

    /// <summary>
    /// Gets the cursor for the page before, or <see langword="null"/> when this is the first.
    /// </summary>
    public TCursor? Previous { get; init; }

    /// <summary>
    /// Gets the cursor for the page after, or <see langword="null"/> when this is the last.
    /// </summary>
    public TCursor? Next { get; init; }
}
