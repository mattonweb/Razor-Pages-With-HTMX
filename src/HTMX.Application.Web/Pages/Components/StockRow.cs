namespace HTMX.Application.Web.Pages.Components;

/// <summary>
/// One row of the Stock table: its item, what its field shows, and the message a refused save left on it.
/// </summary>
public sealed record StockRow
{
    /// <summary>
    /// Gets the row's item.
    /// </summary>
    required public StockItem Item { get; init; }

    /// <summary>
    /// Gets what the row's field shows: the count on hand, or what a refused save posted.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Gets what a refused save says is wrong with the row's field, or <see langword="null"/>.
    /// </summary>
    public string? Message { get; init; }
}
