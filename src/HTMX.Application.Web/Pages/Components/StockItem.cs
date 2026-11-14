namespace HTMX.Application.Web.Pages.Components;

/// <summary>
/// One item on the Stock table page's shelf: its number, its name and how many are on hand.
/// </summary>
public sealed record StockItem
{
    /// <summary>
    /// Gets the item's number, from 10001, which its row's form posts.
    /// </summary>
    required public long Number { get; init; }

    /// <summary>
    /// Gets the item's name.
    /// </summary>
    required public string Name { get; init; }

    /// <summary>
    /// Gets how many are on hand.
    /// </summary>
    required public int OnHand { get; init; }
}
