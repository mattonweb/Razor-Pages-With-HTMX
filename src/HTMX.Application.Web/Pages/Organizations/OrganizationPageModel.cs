namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// What every tab of an organization's page shares: the organization the route names, its header with Archive or
/// Restore, and the toast that says what an action just did. Each tab is a page of its own, with its own URL, that
/// names the partial holding its content.
/// </summary>
/// <remarks>
/// An action runs at once and says so in a toast with Undo, as the brand asks of an action that can be undone. With
/// HTMX the answer is the tab's content and the toast together; without it, a redirect back to the tab, which shows the
/// toast the action left in TempData. Razor Pages finds the handlers declared here on each tab that inherits them.
/// A tab that holds a list keeps where the list is in its query string, and every form and redirect here carries it,
/// so an action leaves the list where it was.
/// </remarks>
public abstract class OrganizationPageModel : PageModel
{
    private const string ArchiveHandler = "Archive";
    private const string RestoreHandler = "Restore";
    private const string ChangedPartial = "_OrganizationChanged";

    // What an Undo that HTMX sends replaces: the tab's content, header included.
    private const string ContentTarget = "#organization";

    // Where a toast waits in TempData across the redirect that follows an action sent without HTMX.
    private const string ToastMessageKey = "ToastMessage";
    private const string ToastUndoKey = "ToastUndo";
    private const string ToastUndoTargetKey = "ToastUndoTarget";

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationPageModel"/> class.
    /// </summary>
    /// <param name="organizations">Reads and changes the organization.</param>
    protected OrganizationPageModel(IOrganizationService organizations)
    {
        ArgumentNullException.ThrowIfNull(organizations);
        this.Organizations = organizations;
    }

    /// <summary>
    /// Gets or sets the route's values, as the browser sent them. Model binding writes them; the page only reads them.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public InputModel Input { get; set; } = new InputModel();

    /// <summary>
    /// Gets the organization the page shows, or <see langword="null"/> when the identifier names none.
    /// </summary>
    public OrganizationSingleResponse? Organization { get; private set; }

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
    /// Gets the service the tab reads and changes the organization through.
    /// </summary>
    protected IOrganizationService Organizations { get; }

    /// <summary>
    /// Reads the organization the route names, what the tab shows of it, and the toast an action left, if any.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>Only the tab's content when HTMX asked for it, as for another page of a list, to swap into the page it
    /// already shows; the whole page otherwise, which says so when no organization has the identifier.</returns>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        // One URL answers with two different bodies, so a cache must keep them apart by the header that chooses.
        HtmxHeaders.VaryByRequestType(this.Response);
        OrganizationSingleResponse? organization = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            // The page still renders, inside the application's frame, and the status tells the browser too.
            this.Response.StatusCode = StatusCodes.Status404NotFound;
            return this.Page();
        }

        if (this.TempData[ToastMessageKey] is string message)
        {
            string target = this.TempData[ToastUndoTargetKey] as string ?? ContentTarget;
            this.Toast = CreateToast(message, this.TempData[ToastUndoKey] as string, target);
        }

        return HtmxAnswers.PartOrPage(this, this.ContentPartial);
    }

    /// <summary>
    /// Gives the route and query string values that bring the tab back as it is shown: the organization's identifier
    /// and, on a tab that holds a list, where the list is. The tab's forms post to them, and an action without HTMX is
    /// redirected to them.
    /// </summary>
    /// <returns>The values, by name.</returns>
    public virtual Dictionary<string, string> TabValues()
    {
        return new Dictionary<string, string> { ["id"] = this.Input.Id.ToString(CultureInfo.InvariantCulture) };
    }

    /// <summary>
    /// Archives the organization, and says so in a toast whose Undo restores it.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise.</returns>
    public Task<IActionResult> OnPostArchiveAsync(CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(ChangeKind.Archived, cancellationToken);
    }

    /// <summary>
    /// Restores the organization, and says so in a toast whose Undo archives it again.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise.</returns>
    public Task<IActionResult> OnPostRestoreAsync(CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(ChangeKind.Restored, cancellationToken);
    }

    /// <summary>
    /// Reads, after the organization, whatever else the tab shows of it. The Details tab needs nothing more.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>A task that completes when it is read.</returns>
    protected virtual Task ReadMoreAsync(OrganizationId id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads the organization the route names and, when there is one, what the tab shows of it.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The organization, or <see langword="null"/> when there is none.</returns>
    protected async Task<OrganizationSingleResponse?> ReadAsync(CancellationToken cancellationToken)
    {
        OrganizationId id = new OrganizationId(this.Input.Id);
        this.Organization = await this.Organizations.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (this.Organization is not null)
        {
            await this.ReadMoreAsync(id, cancellationToken).ConfigureAwait(false);
        }

        return this.Organization;
    }

    /// <summary>
    /// Writes the address of one of the tab's handlers, for the organization the route names, with any other values in
    /// the query string, such as an Undo's.
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
    /// Answers an action on the organization. HTMX gets the tab's content and the toast, to swap in place. A browser
    /// without HTMX is redirected back to the tab, so that refreshing it does not post again, and the toast waits in
    /// TempData for the tab to show.
    /// </summary>
    /// <param name="message">The toast's message.</param>
    /// <param name="undoAction">The address Undo posts to, or <see langword="null"/> for no Undo.</param>
    /// <returns>The content and toast, or the redirect.</returns>
    protected IActionResult Answer(string message, string? undoAction)
    {
        return this.Answer(message, undoAction, ChangedPartial, ContentTarget);
    }

    /// <summary>
    /// Answers an action on one part of the tab, such as a row of its list: HTMX gets that part and the toast, and the
    /// toast's Undo replaces that part again. Without HTMX it is the redirect back to the tab, as for an action on the
    /// organization.
    /// </summary>
    /// <param name="message">The toast's message.</param>
    /// <param name="undoAction">The address Undo posts to, or <see langword="null"/> for no Undo.</param>
    /// <param name="changedPartial">The partial that holds the part and the toast.</param>
    /// <param name="undoTarget">The element the part replaces, which an Undo that HTMX sends replaces too.</param>
    /// <returns>The part and toast, or the redirect.</returns>
    protected IActionResult Answer(string message, string? undoAction, string changedPartial, string undoTarget)
    {
        if (HtmxHeaders.IsPartialRequest(this.Request))
        {
            this.Toast = CreateToast(message, undoAction, undoTarget);
            return this.Partial(changedPartial, this);
        }

        this.TempData[ToastMessageKey] = message;
        this.TempData[ToastUndoKey] = undoAction;
        this.TempData[ToastUndoTargetKey] = undoTarget;
        return this.RedirectToPage(this.PageName, this.TabValues());
    }

    /// <summary>
    /// Goes to the organization's own page after an action found it gone, so that the page says so.
    /// </summary>
    /// <returns>A redirect, or 204 with the request to load the page.</returns>
    protected IActionResult ShowGone()
    {
        return HtmxAnswers.NextPage(this, "./Details", new { id = this.Input.Id });
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
    /// Writes the toast's message for what became of an archive or a restore.
    /// </summary>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="outcome">What became of it.</param>
    /// <param name="name">The organization's name.</param>
    /// <returns>The message.</returns>
    private static string StatusMessage(ChangeKind change, UpdateOutcome outcome, OrganizationName name)
    {
        bool archiving = change == ChangeKind.Archived;
        return outcome switch
        {
            UpdateOutcome.Saved =>
                archiving ? $"{name.Value} archived." : $"{name.Value} restored.",
            UpdateOutcome.Unchanged =>
                archiving ? $"{name.Value} was already archived." : $"{name.Value} was already active.",
            _ =>
                $"{name.Value} was changed by someone else at the same moment. Try again.",
        };
    }

    /// <summary>
    /// Archives or restores the organization, reads it again, and answers with what it now is and a toast that says
    /// what happened. When the organization no longer exists, its page says so.
    /// </summary>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise.</returns>
    private async Task<IActionResult> ChangeStatusAsync(ChangeKind change, CancellationToken cancellationToken)
    {
        OrganizationStatusRequest request = new OrganizationStatusRequest
        {
            Id = new OrganizationId(this.Input.Id),
            ChangedBy = SignedInUser.Of(this.User),
        };
        UpdateOutcome outcome = change == ChangeKind.Archived
            ? await this.Organizations.ArchiveAsync(request, cancellationToken).ConfigureAwait(false)
            : await this.Organizations.RestoreAsync(request, cancellationToken).ConfigureAwait(false);
        OrganizationSingleResponse? organization = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return this.ShowGone();
        }

        // Undo is offered only for a change that was made: it posts the opposite change.
        string? undo = null;
        if (outcome == UpdateOutcome.Saved)
        {
            string opposite = change == ChangeKind.Archived ? RestoreHandler : ArchiveHandler;
            undo = this.HandlerAddress(opposite, this.TabValues());
        }

        return this.Answer(StatusMessage(change, outcome, organization.Name), undo);
    }

    /// <summary>
    /// The route's values, as the browser sent them.
    /// </summary>
    public sealed class InputModel
    {
        /// <summary>
        /// Gets or sets the identifier of the organization to show.
        /// </summary>
        public long Id { get; set; }
    }
}
