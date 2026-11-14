namespace HTMX.Application.Web.Pages.Components;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Seen notices page, at /components/seen-notices: a list of notices, each marking itself seen as it scrolls into
/// view, and buttons that mark them all seen or all new.
/// </summary>
/// <remarks>
/// A notice that comes into view is a request nobody sent with a form: HTMX sends it, by the <c>revealed</c> trigger,
/// from an element with no form around it, so no form brings the antiforgery token. The list sends it in a header
/// instead, through an inherited <c>hx-headers</c>. Without HTMX nothing marks itself seen, and Mark all as seen,
/// a form, does it.
/// </remarks>
public sealed class SeenNoticesModel : PageModel
{
    // The list's card, which the page draws and the two buttons' answers draw alone.
    private const string ListPartial = "_NoticeList";

    private readonly NoticeBoard _board;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeenNoticesModel"/> class.
    /// </summary>
    /// <param name="board">The notices, which the list shows and marks.</param>
    public SeenNoticesModel(NoticeBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        this._board = board;
    }

    /// <summary>
    /// Gets the notices the list shows, in order.
    /// </summary>
    public Notice[] Notices { get; private set; } = [];

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Shows the list.
    /// </summary>
    public void OnGet()
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        this.Notices = this._board.All();
    }

    /// <summary>
    /// Marks one notice seen, as HTMX asks when the notice comes into view. The answer has nothing to draw: the notice
    /// keeps its New chip until the list is drawn again, so that it does not change while it is being read.
    /// </summary>
    /// <param name="notice">The notice's number, which its request names in its address.</param>
    /// <returns>204 No Content, or 404 for a notice the board does not have.</returns>
    public IActionResult OnPostSeen(long notice)
    {
        return this._board.MarkSeen(notice) ? this.StatusCode(StatusCodes.Status204NoContent) : this.NotFound();
    }

    /// <summary>
    /// Marks every notice seen.
    /// </summary>
    /// <returns>The list for HTMX, or the redirect back to the page.</returns>
    public IActionResult OnPostAllSeen()
    {
        this._board.MarkAll(seen: true);
        return this.Redraw();
    }

    /// <summary>
    /// Marks every notice new again, so the list can be tried once more.
    /// </summary>
    /// <returns>The list for HTMX, or the redirect back to the page.</returns>
    public IActionResult OnPostAllNew()
    {
        this._board.MarkAll(seen: false);
        return this.Redraw();
    }

    /// <summary>
    /// Draws the list again after a button: the list alone for HTMX, and, without it, a redirect back to the page, so
    /// that a refresh does not post again.
    /// </summary>
    /// <returns>The list or the redirect.</returns>
    private IActionResult Redraw()
    {
        if (!HtmxHeaders.IsPartialRequest(this.Request))
        {
            return this.RedirectToPage();
        }

        this.Notices = this._board.All();
        return this.Partial(ListPartial, this);
    }
}
