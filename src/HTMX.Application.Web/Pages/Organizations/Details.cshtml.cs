namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// One organization's own page, at /organizations/{id}: its name, status and version, who changed it last, and every
/// value it holds, with Archive or Restore. An identifier that names no organization gets the page saying so, with the
/// status 404 Not Found.
/// </summary>
/// <remarks>
/// Archive and Restore run at once and say so in a toast with Undo, as the brand asks of an action that can be undone.
/// With HTMX the answer is the page's content and the toast together; without it, a redirect back to the page, which
/// shows the toast the action left in TempData.
/// </remarks>
public sealed class DetailsModel : PageModel
{
    private const string DetailsPage = "./Details";
    private const string ArchiveHandler = "Archive";
    private const string RestoreHandler = "Restore";
    private const string StatusChangedPartial = "_OrganizationStatusChanged";

    // What an Undo that HTMX sends replaces: the page's content.
    private const string DetailsTarget = "#organization-details";

    // Where a toast waits in TempData across the redirect that follows an action sent without HTMX.
    private const string ToastMessageKey = "ToastMessage";
    private const string ToastUndoKey = "ToastUndo";

    private readonly IOrganizationService _organizations;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="organizations">Reads, archives and restores the organization.</param>
    public DetailsModel(IOrganizationService organizations)
    {
        ArgumentNullException.ThrowIfNull(organizations);
        this._organizations = organizations;
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
    /// Reads the organization the route names, and the toast an action left, if any.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The page, which says so when no organization has the identifier.</returns>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        OrganizationSingleResponse? organization = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            // The page still renders, inside the application's frame, and the status tells the browser too.
            this.Response.StatusCode = StatusCodes.Status404NotFound;
            return this.Page();
        }

        if (this.TempData[ToastMessageKey] is string message)
        {
            this.Toast = this.CreateToast(message, this.TempData[ToastUndoKey] as string);
        }

        return this.Page();
    }

    /// <summary>
    /// Archives the organization, and says so in a toast whose Undo restores it.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The page's content and the toast for HTMX; a redirect back to the page otherwise.</returns>
    public Task<IActionResult> OnPostArchiveAsync(CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(ChangeKind.Archived, cancellationToken);
    }

    /// <summary>
    /// Restores the organization, and says so in a toast whose Undo archives it again.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The page's content and the toast for HTMX; a redirect back to the page otherwise.</returns>
    public Task<IActionResult> OnPostRestoreAsync(CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(ChangeKind.Restored, cancellationToken);
    }

    /// <summary>
    /// Writes the toast's message for what became of an archive or a restore.
    /// </summary>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="outcome">What became of it.</param>
    /// <param name="name">The organization's name.</param>
    /// <returns>The message.</returns>
    private static string ToastMessage(ChangeKind change, UpdateOutcome outcome, OrganizationName name)
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
    /// <returns>The page's content and the toast for HTMX; a redirect back to the page otherwise.</returns>
    private async Task<IActionResult> ChangeStatusAsync(ChangeKind change, CancellationToken cancellationToken)
    {
        OrganizationStatusRequest request = new OrganizationStatusRequest
        {
            Id = new OrganizationId(this.Input.Id),
            ChangedBy = SignedInUser.Of(this.User),
        };
        UpdateOutcome outcome = change == ChangeKind.Archived
            ? await this._organizations.ArchiveAsync(request, cancellationToken).ConfigureAwait(false)
            : await this._organizations.RestoreAsync(request, cancellationToken).ConfigureAwait(false);
        OrganizationSingleResponse? organization = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return HtmxAnswers.NextPage(this, DetailsPage, new { id = this.Input.Id });
        }

        // Undo is offered only for a change that was made: it posts the opposite change.
        string? undo = null;
        if (outcome == UpdateOutcome.Saved)
        {
            undo = change == ChangeKind.Archived ? RestoreHandler : ArchiveHandler;
        }

        string message = ToastMessage(change, outcome, organization.Name);
        return this.Answer(message, undo);
    }

    /// <summary>
    /// Answers an archive or restore. HTMX gets the page's content and the toast, to swap in place. A browser without
    /// HTMX is redirected back to the page, so that refreshing it does not post again, and the toast waits in TempData
    /// for that page to show.
    /// </summary>
    /// <param name="message">The toast's message.</param>
    /// <param name="undoHandler">The handler Undo posts to, or <see langword="null"/> for no Undo.</param>
    /// <returns>The content and toast, or the redirect.</returns>
    private IActionResult Answer(string message, string? undoHandler)
    {
        if (HtmxHeaders.IsPartialRequest(this.Request))
        {
            this.Toast = this.CreateToast(message, undoHandler);
            return this.Partial(StatusChangedPartial, this);
        }

        this.TempData[ToastMessageKey] = message;
        this.TempData[ToastUndoKey] = undoHandler;
        return this.RedirectToPage(DetailsPage, new { id = this.Input.Id });
    }

    /// <summary>
    /// Makes a toast whose Undo, if any, posts to one of this page's handlers for the organization the route names.
    /// </summary>
    /// <param name="message">The toast's message.</param>
    /// <param name="undoHandler">The handler Undo posts to, or <see langword="null"/> for no Undo.</param>
    /// <returns>The toast.</returns>
    private Toast CreateToast(string message, string? undoHandler)
    {
        if (undoHandler is null)
        {
            return new Toast { Message = message };
        }

        return new Toast
        {
            Message = message,
            UndoAction = this.Url.Page(DetailsPage, undoHandler, new { id = this.Input.Id }),
            UndoTarget = DetailsTarget,
        };
    }

    /// <summary>
    /// Reads the organization the route names.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The organization, or <see langword="null"/> when there is none.</returns>
    private async Task<OrganizationSingleResponse?> ReadAsync(CancellationToken cancellationToken)
    {
        OrganizationId id = new OrganizationId(this.Input.Id);
        this.Organization = await this._organizations.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return this.Organization;
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
