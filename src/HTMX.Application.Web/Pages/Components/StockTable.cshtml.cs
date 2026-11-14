namespace HTMX.Application.Web.Pages.Components;

using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Stock table page, at /components/stock-table: a table of items, each row saving how many of its item are on
/// hand with a form of its own.
/// </summary>
/// <remarks>
/// A form cannot hold a table row's cells, so each row's form sits in the row's last cell, around its Save button, and
/// the row's field, in another cell, names that form with the <c>form</c> attribute. The browser posts the field with
/// the form, and so does HTMX, which finds an element's form as the browser does; the form brings the antiforgery
/// token. Without HTMX a save is redirected back to the page; with HTMX the answer is the row and a toast.
/// </remarks>
public sealed class StockTableModel : PageModel
{
    // A row alone, which a refused save answers HTMX with, and a row saved, with its toast.
    private const string RowPartial = "_StockRow";
    private const string SavedPartial = "_StockSaved";

    // The field every row's form posts, by the name model binding gives its value and its messages.
    private const string OnHandField = "Input.OnHand";

    private readonly StockShelf _shelf;

    /// <summary>
    /// Initializes a new instance of the <see cref="StockTableModel"/> class.
    /// </summary>
    /// <param name="shelf">The items, which the table lists and each row's form saves to.</param>
    public StockTableModel(StockShelf shelf)
    {
        ArgumentNullException.ThrowIfNull(shelf);
        this._shelf = shelf;
    }

    /// <summary>
    /// Gets or sets the values the row's form posted.
    /// </summary>
    [BindProperty]
    public StockInput Input { get; set; } = new StockInput();

    /// <summary>
    /// Gets the table's rows, one for each item, in order.
    /// </summary>
    public StockRow[] Rows { get; private set; } = [];

    /// <summary>
    /// Gets the row a save answers HTMX with, or <see langword="null"/>.
    /// </summary>
    public StockRow? Row { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Gets the toast that says what was saved, or <see langword="null"/>.
    /// </summary>
    public Toast? Toast { get; private set; }

    /// <summary>
    /// Shows the table, and the toast a save without HTMX left before its redirect.
    /// </summary>
    public void OnGet()
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        this.Toast = TempDataToasts.Take(this.TempData, "#stock");
        this.Rows = Array.ConvertAll(this._shelf.Items(), CountedRow);
    }

    /// <summary>
    /// Saves how many of one item are on hand. An item the shelf does not have answers 404. A count that fails answers
    /// 422 with the row, its field marked. A count saved answers HTMX with the row and a toast, and the browser on its
    /// own with a redirect back to the page, which shows the toast.
    /// </summary>
    /// <param name="item">The item's number, which the row's form names in its address.</param>
    /// <returns>The row, the page or the redirect.</returns>
    public IActionResult OnPostSave(long item)
    {
        StockItem? found = this._shelf.Find(item);
        if (found is null)
        {
            return this.NotFound();
        }

        if (!this.ModelState.IsValid)
        {
            return this.Refuse(found);
        }

        StockItem counted = this._shelf.Count(item, this.Input.OnHand ?? 0) ?? found;
        string onHand = counted.OnHand.ToString("N0", CultureInfo.CurrentCulture);
        Toast toast = new Toast { Message = $"{counted.Name}: {onHand} on hand." };
        if (!HtmxHeaders.IsPartialRequest(this.Request))
        {
            TempDataToasts.Leave(this.TempData, toast);
            return this.RedirectToPage();
        }

        this.Row = CountedRow(counted);
        this.Toast = toast;
        return this.Partial(SavedPartial, this);
    }

    /// <summary>
    /// Draws an item's row as the shelf holds it.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The row, its field showing the count on hand.</returns>
    private static StockRow CountedRow(StockItem item)
    {
        return new StockRow { Item = item, Value = item.OnHand.ToString(CultureInfo.InvariantCulture) };
    }

    /// <summary>
    /// Answers a refused save with 422: the row alone for HTMX, the whole page otherwise, the row's field showing what
    /// was posted and marked with the message.
    /// </summary>
    /// <param name="item">The item whose row was refused.</param>
    /// <returns>The row or the page.</returns>
    private IActionResult Refuse(StockItem item)
    {
        ModelStateEntry? entry = this.ModelState[OnHandField];
        StockRow refused = new StockRow
        {
            Item = item,
            Value = entry?.AttemptedValue,
            Message = entry?.Errors.Count > 0 ? entry.Errors[0].ErrorMessage : null,
        };
        this.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        this.Rows = Array.ConvertAll(
            this._shelf.Items(),
            shelved => shelved.Number == item.Number ? refused : CountedRow(shelved));
        this.Row = refused;
        return HtmxHeaders.IsPartialRequest(this.Request) ? this.Partial(RowPartial, refused) : this.Page();
    }
}
