namespace HTMX.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// The Order lines page, at /components/order-lines: a form of lines, each a product and a quantity, which the user
/// adds to and takes from, and which says what order it received. It keeps nothing.
/// </summary>
/// <remarks>
/// A line holds two fields, so its fields post with the line's position in their names, <c>Input.Lines[1].Quantity</c>,
/// and bind to an array in that order. Add and Remove post the form to handlers of their own, which save nothing and
/// draw the lines again, numbered afresh from 0: a removal from the middle leaves no gap, which would stop the binding
/// there. They clear ModelState first, because a tag helper writes a posted value ahead of the model's, by name, and
/// after a removal the line now at position 1 would show what the removed line at position 1 posted.
/// </remarks>
public sealed class OrderLinesModel : PageModel
{
    /// <summary>
    /// The most lines an order may have; the form offers no more.
    /// </summary>
    public const int MostLines = 5;

    // The form's card, the lines alone, and the answer to HTMX for an order received: the form, new, and a toast.
    private const string FormPartial = "_OrderForm";
    private const string LinesPartial = "_OrderLines";
    private const string ReceivedPartial = "_OrderReceived";

    /// <summary>
    /// Gets the product select's options, each product by name.
    /// </summary>
    public static IEnumerable<SelectListItem> ProductOptions =>
        OrderProducts.Names.Select(static name => new SelectListItem(name, name));

    /// <summary>
    /// Gets or sets the form's values, as the browser posted them.
    /// </summary>
    [BindProperty]
    public OrderInput Input { get; set; } = new OrderInput();

    /// <summary>
    /// Gets the position of the line whose product takes the keyboard: the line Add made, the line now in the place of
    /// the one Remove took away, or the first line of a new form after an order was received. None otherwise.
    /// </summary>
    public int? FocusedLine { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Gets the toast that says what order was received, or <see langword="null"/>.
    /// </summary>
    public Toast? Toast { get; private set; }

    /// <summary>
    /// Shows a new form, and the toast an order received without HTMX left before its redirect.
    /// </summary>
    public void OnGet()
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        this.Toast = TempDataToasts.Take(this.TempData, "#order-form");
    }

    /// <summary>
    /// Receives the order: checks every line, and keeps nothing. A form that fails answers 422 with the form, each
    /// wrong field marked. An order received answers HTMX with a new form and a toast, and the browser on its own with
    /// a redirect back to the page, which shows the toast.
    /// </summary>
    /// <returns>The form, or the redirect.</returns>
    public IActionResult OnPost()
    {
        this.CheckProducts();
        if (!this.ModelState.IsValid)
        {
            this.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return HtmxAnswers.PartOrPage(this, FormPartial);
        }

        int lines = this.Input.Lines.Length;
        int items = this.Input.Lines.Sum(static line => line.Quantity ?? 0);
        Toast toast = new Toast
        {
            Message = $"Received an order of {Count(lines, "line")} and {Count(items, "item")}.",
        };
        if (!HtmxHeaders.IsPartialRequest(this.Request))
        {
            TempDataToasts.Leave(this.TempData, toast);
            return this.RedirectToPage();
        }

        this.ModelState.Clear();
        this.Input = new OrderInput();
        this.Toast = toast;
        this.FocusedLine = 0;
        return this.Partial(ReceivedPartial, this);
    }

    /// <summary>
    /// Adds an empty line, which takes the keyboard, unless the order already has the most it may, and saves nothing.
    /// </summary>
    /// <returns>The lines for HTMX, or the whole page.</returns>
    public IActionResult OnPostAddLine()
    {
        this.ModelState.Clear();
        if (this.Input.Lines.Length < MostLines)
        {
            this.Input.Lines = [.. this.Input.Lines, new OrderLineInput()];
        }

        this.FocusedLine = this.Input.Lines.Length - 1;
        return this.Redraw();
    }

    /// <summary>
    /// Takes a line away, unless it is the only one, and gives the keyboard to the line now in its place, or to the
    /// new last line when it was last. The lines left are numbered afresh, and nothing is saved.
    /// </summary>
    /// <param name="removeLine">The line's position, from 0, which its Remove button posts.</param>
    /// <returns>The lines for HTMX, or the whole page.</returns>
    public IActionResult OnPostRemoveLine(int removeLine)
    {
        this.ModelState.Clear();
        List<OrderLineInput> lines = [.. this.Input.Lines];
        if (lines.Count > 1 && removeLine >= 0 && removeLine < lines.Count)
        {
            lines.RemoveAt(removeLine);
        }

        this.Input.Lines = [.. lines];
        this.FocusedLine = Math.Clamp(removeLine, 0, lines.Count - 1);
        return this.Redraw();
    }

    /// <summary>
    /// Writes a count with its noun, one or many.
    /// </summary>
    /// <param name="count">The count.</param>
    /// <param name="noun">The noun for one, such as <c>line</c>.</param>
    /// <returns>The words, such as <c>2 lines</c>.</returns>
    private static string Count(int count, string noun)
    {
        return count == 1 ? $"1 {noun}" : $"{count} {noun}s";
    }

    /// <summary>
    /// Marks a line whose product is not one the form offers, as a post the select did not send can name.
    /// </summary>
    private void CheckProducts()
    {
        for (int position = 0; position < this.Input.Lines.Length; position++)
        {
            string? product = this.Input.Lines[position].Product;
            if (product is not null && !OrderProducts.Names.Contains(product))
            {
                this.ModelState.AddModelError($"Input.Lines[{position}].Product", "Choose a product from the list.");
            }
        }
    }

    /// <summary>
    /// Draws the form again after Add or Remove: the lines alone for HTMX, the whole page otherwise.
    /// </summary>
    /// <returns>The lines or the page.</returns>
    private IActionResult Redraw()
    {
        return HtmxHeaders.IsPartialRequest(this.Request) ? this.Partial(LinesPartial, this) : this.Page();
    }
}
