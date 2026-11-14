namespace HTMX.Application.Web.Pages.Components;

using System;

/// <summary>
/// The Stock table page's shelf: a few invented items and how many of each are on hand, one shelf for everyone who
/// opens the page.
/// </summary>
public sealed class StockShelf
{
    // The items, in the order the table lists them. Every read and write locks it.
    private readonly StockItem[] _items =
    [
        new StockItem { Number = 10001, Name = "Placeholder pens", OnHand = 120 },
        new StockItem { Number = 10002, Name = "Sample paper", OnHand = 40 },
        new StockItem { Number = 10003, Name = "Mock mugs", OnHand = 12 },
        new StockItem { Number = 10004, Name = "Dummy desk lamps", OnHand = 3 },
    ];

    /// <summary>
    /// Reads every item, in the order the table lists them.
    /// </summary>
    /// <returns>The items.</returns>
    public StockItem[] Items()
    {
        lock (this._items)
        {
            return [.. this._items];
        }
    }

    /// <summary>
    /// Finds an item by its number.
    /// </summary>
    /// <param name="number">The item's number.</param>
    /// <returns>The item, or <see langword="null"/> when the shelf has none by that number.</returns>
    public StockItem? Find(long number)
    {
        lock (this._items)
        {
            return Array.Find(this._items, item => item.Number == number);
        }
    }

    /// <summary>
    /// Records how many of an item are on hand.
    /// </summary>
    /// <param name="number">The item's number.</param>
    /// <param name="onHand">How many are on hand, from 0.</param>
    /// <returns>The item as it is now, or <see langword="null"/> when the shelf has none by that number.</returns>
    public StockItem? Count(long number, int onHand)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(onHand);
        lock (this._items)
        {
            int position = Array.FindIndex(this._items, item => item.Number == number);
            if (position < 0)
            {
                return null;
            }

            this._items[position] = Counted(this._items[position], onHand);
            return this._items[position];
        }
    }

    /// <summary>
    /// Records how many of an item are on hand.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="onHand">How many are on hand.</param>
    /// <returns>The item, so counted.</returns>
    private static StockItem Counted(StockItem item, int onHand)
    {
        return item with { OnHand = onHand };
    }
}
