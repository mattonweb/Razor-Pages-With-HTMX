namespace HTMX.Application.Web.Pages;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// What every tab of one record's page shares, whatever the record: the identifier the route names, the toast an
/// action leaves, and the two answers an action gives. Each tab is a page of its own, with its own URL, that names the
/// partial holding its content. A base page model for one kind of record, such as an organization, reads the record
/// and holds the actions every one of its tabs offers.
/// </summary>
/// <remarks>
/// An action runs at once and says so in a toast with Undo, as the brand asks of an action that can be undone. With
/// HTMX the answer is the tab's content and the toast together; without it, a redirect back to the tab, which shows the
/// toast the action left in TempData. Razor Pages finds the handlers declared on a base page model on each tab that
/// inherits them. A tab that holds a list keeps where the list is in its query string, and every form and redirect
/// here carries it, so an action leaves the list where it was.
/// </remarks>
public abstract class RecordPageModel : PageModel
{
    // What HTMX gets after an action on the record: the tab's content and the toast.
    private const string ChangedPartial = "_RecordChanged";

    /// <summary>
    /// Gets or sets the route's values, as the browser sent them. Model binding writes them; the page only reads them.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public InputModel Input { get; set; } = new InputModel();

    /// <summary>
    /// Gets the toast that says what an action just did, or <see langword="null"/> when none has run.
    /// </summary>
    public Toast? Toast { get; private set; }

    /// <summary>
    /// Gets the tab's page, such as <c>./Details</c>, which its forms post to and its tab link marks as current.
    /// </summary>
    public abstract string PageName { get; }

    /// <summary>
    /// Gets the partial that holds the tab's content, which the page renders and an action sent by HTMX answers with.
    /// </summary>
    public abstract string ContentPartial { get; }

    /// <summary>
    /// Gets the element that holds the tab's content, such as <c>#organization</c>, which an action on the record and
    /// its Undo replace.
    /// </summary>
    protected abstract string ContentTarget { get; }

    /// <summary>
    /// Reads the record the route names, what the tab shows of it, and the toast an action left, if any.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>Only the tab's content when HTMX asked for it, as for another page of a list, to swap into the page it
    /// already shows; the whole page otherwise, which says so when no record has the identifier.</returns>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        // One URL answers with two different bodies, so a cache must keep them apart by the header that chooses.
        HtmxHeaders.VaryByRequestType(this.Response);
        bool found = await this.FindRecordAsync(cancellationToken).ConfigureAwait(false);
        if (!found)
        {
            // The page still renders, inside the application's frame, and the status tells the browser too.
            this.Response.StatusCode = StatusCodes.Status404NotFound;
            return this.Page();
        }

        this.Toast = TempDataToasts.Take(this.TempData, this.ContentTarget);
        return HtmxAnswers.PartOrPage(this, this.ContentPartial);
    }

    /// <summary>
    /// Gives the route and query string values that bring the tab back as it is shown: the record's identifier and,
    /// on a tab that holds a list, where the list is. The tab's forms post to them, and an action without HTMX is
    /// redirected to them.
    /// </summary>
    /// <returns>The values, by name.</returns>
    public virtual Dictionary<string, string> TabValues()
    {
        return new Dictionary<string, string> { ["id"] = this.Input.Id.ToString(CultureInfo.InvariantCulture) };
    }

    /// <summary>
    /// Reads the record the route names and, when there is one, what the tab shows of it.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns><see langword="true"/> when a record has the identifier.</returns>
    protected abstract Task<bool> FindRecordAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Writes the address of one of the tab's handlers, for the record the route names, with any other values in the
    /// query string, such as an Undo's.
    /// </summary>
    /// <param name="handler">The handler, such as <c>Restore</c>.</param>
    /// <param name="values">The route's and query string's values, the identifier among them.</param>
    /// <returns>The address.</returns>
    protected string HandlerAddress(string handler, object values)
    {
        return this.Url.Page(this.PageName, handler, values)
            ?? throw new InvalidOperationException($"The handler {handler} has no address.");
    }

    /// <summary>
    /// Answers an action on the record. HTMX gets the tab's content and the toast, to swap in place. A browser without
    /// HTMX is redirected back to the tab, so that refreshing it does not post again, and the toast waits in TempData
    /// for the tab to show.
    /// </summary>
    /// <param name="message">The toast's message.</param>
    /// <param name="undoAction">The address Undo posts to, or <see langword="null"/> for no Undo.</param>
    /// <returns>The content and toast, or the redirect.</returns>
    protected IActionResult Answer(string message, string? undoAction)
    {
        return this.Answer(message, undoAction, ChangedPartial, this.ContentTarget);
    }

    /// <summary>
    /// Answers an action on one part of the tab, such as a row of its list: HTMX gets that part and the toast, and the
    /// toast's Undo replaces that part again. Without HTMX it is the redirect back to the tab, as for an action on the
    /// record.
    /// </summary>
    /// <param name="message">The toast's message.</param>
    /// <param name="undoAction">The address Undo posts to, or <see langword="null"/> for no Undo.</param>
    /// <param name="changedPartial">The partial that holds the part and the toast.</param>
    /// <param name="undoTarget">The element the part replaces, which an Undo that HTMX sends replaces too.</param>
    /// <returns>The part and toast, or the redirect.</returns>
    protected IActionResult Answer(string message, string? undoAction, string changedPartial, string undoTarget)
    {
        Toast toast = CreateToast(message, undoAction, undoTarget);
        if (HtmxHeaders.IsPartialRequest(this.Request))
        {
            this.Toast = toast;
            return this.Partial(changedPartial, this);
        }

        TempDataToasts.Leave(this.TempData, toast);
        return this.RedirectToPage(this.PageName, this.TabValues());
    }

    /// <summary>
    /// Makes a toast whose Undo, if any, replaces an element of the tab when HTMX sends it.
    /// </summary>
    /// <param name="message">The toast's message.</param>
    /// <param name="undoAction">The address Undo posts to, or <see langword="null"/> for no Undo.</param>
    /// <param name="undoTarget">The element Undo replaces, such as <c>#organization</c>.</param>
    /// <returns>The toast.</returns>
    private static Toast CreateToast(string message, string? undoAction, string undoTarget)
    {
        return new Toast
        {
            Message = message,
            UndoAction = undoAction,
            UndoTarget = undoAction is null ? null : undoTarget,
        };
    }

    /// <summary>
    /// The route's values, as the browser sent them.
    /// </summary>
    public sealed class InputModel
    {
        /// <summary>
        /// Gets or sets the identifier of the record to show.
        /// </summary>
        public long Id { get; set; }
    }
}
