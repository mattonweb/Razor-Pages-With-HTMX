namespace HTMX.Application.Web.Pages.Users;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// What every tab of a user's page shares: the user the route names, and their header with Archive or Restore. The
/// identifier, the toast and the answers an action gives are <see cref="RecordPageModel"/>'s.
/// </summary>
/// <remarks>
/// A page model's own <c>User</c> is whoever is signed in, so the user the page shows is its <see cref="Account"/>.
/// Nobody may archive themselves, since they would be shut out: the signed-in user's own page offers no Archive, and
/// the service refuses one sent anyway. An archived user can be deleted permanently, after a confirm dialog, unless
/// other records name them; then the dialog says why not.
/// </remarks>
public abstract class UserPageModel : RecordPageModel
{
    private const string ArchiveHandler = "Archive";
    private const string RestoreHandler = "Restore";

    // Where a deleted user's page goes: the list, which shows the toast the delete left.
    private const string ListPage = "./Index";

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPageModel"/> class.
    /// </summary>
    /// <param name="users">Reads and changes the user.</param>
    protected UserPageModel(IUserService users)
    {
        ArgumentNullException.ThrowIfNull(users);
        this.Users = users;
    }

    /// <summary>
    /// Gets the user the page shows, or <see langword="null"/> when the identifier names none.
    /// </summary>
    public UserSingleResponse? Account { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page shows the signed-in user, who may not archive themselves.
    /// </summary>
    public bool IsSignedInUser => this.Account is UserSingleResponse account && account.Id == SignedInUser.Of(this.User);

    /// <summary>
    /// Gets a value indicating whether other records name the user, which keeps them from being deleted. It is read
    /// only for an archived user, the only kind that can be deleted, and is <see langword="false"/> otherwise.
    /// </summary>
    public bool InUse { get; private set; }

    /// <summary>
    /// Gets the service the tab reads and changes the user through.
    /// </summary>
    protected IUserService Users { get; }

    /// <inheritdoc/>
    protected override string ContentTarget => "#user";

    /// <summary>
    /// Archives the user, and says so in a toast whose Undo restores them.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise.</returns>
    public Task<IActionResult> OnPostArchiveAsync(CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(ChangeKind.Archived, cancellationToken);
    }

    /// <summary>
    /// Restores the user, and says so in a toast whose Undo archives them again.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise.</returns>
    public Task<IActionResult> OnPostRestoreAsync(CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(ChangeKind.Restored, cancellationToken);
    }

    /// <summary>
    /// Deletes the user permanently, with their history, from the version the confirm dialog showed them at, and goes
    /// to the users list, which says so in a toast with no Undo. A delete the service refuses answers as Archive does,
    /// with the tab's content and a toast that says why.
    /// </summary>
    /// <param name="version">The version the page showed the user at, from the confirm dialog's form.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The list, by a redirect or by asking HTMX to load it; the tab's content and a toast for a refusal; 400
    /// for a version number that cannot be one.</returns>
    public async Task<IActionResult> OnPostDeleteAsync(int version, CancellationToken cancellationToken)
    {
        if (version < 1)
        {
            return this.BadRequest();
        }

        // Read first, for the name the toast gives: once the user is deleted, nothing holds it.
        UserSingleResponse? before = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (before is null)
        {
            return this.ShowGone();
        }

        UserDeleteRequest request = new UserDeleteRequest { Id = before.Id, Version = new VersionNumber(version) };
        DeleteOutcome outcome = await this.Users.DeleteAsync(request, cancellationToken).ConfigureAwait(false);
        if (outcome == DeleteOutcome.Deleted)
        {
            TempDataToasts.Leave(this.TempData, new Toast { Message = $"{before.DisplayName.Value} deleted." });
            return HtmxAnswers.NextPage(this, ListPage, new { });
        }

        UserSingleResponse? account = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return this.ShowGone();
        }

        return this.Answer(DeleteMessage(outcome, account.DisplayName), undoAction: null);
    }

    /// <inheritdoc/>
    protected override async Task<bool> FindRecordAsync(CancellationToken cancellationToken)
    {
        UserSingleResponse? account = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        return account is not null;
    }

    /// <summary>
    /// Reads, after the user, whatever else the tab shows of them. The Details tab needs nothing more.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>A task that completes when it is read.</returns>
    protected virtual Task ReadMoreAsync(UserId id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads the user the route names and, when there is one, what the tab shows of them.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The user, or <see langword="null"/> when there is none.</returns>
    protected async Task<UserSingleResponse?> ReadAsync(CancellationToken cancellationToken)
    {
        UserId id = new UserId(this.Input.Id);
        this.Account = await this.Users.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (this.Account is null)
        {
            return null;
        }

        // Only an archived user can be deleted, so only their page asks whether other records name them.
        this.InUse = this.Account.Archived is not null
            && await this.Users.IsInUseAsync(id, cancellationToken).ConfigureAwait(false);
        await this.ReadMoreAsync(id, cancellationToken).ConfigureAwait(false);
        return this.Account;
    }

    /// <summary>
    /// Goes to the user's own page after an action found them gone, so that the page says so.
    /// </summary>
    /// <returns>A redirect, or 204 with the request to load the page.</returns>
    protected IActionResult ShowGone()
    {
        return HtmxAnswers.NextPage(this, "./Details", new { id = this.Input.Id });
    }

    /// <summary>
    /// Writes the toast's message for what became of an archive or a restore.
    /// </summary>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="outcome">What became of it.</param>
    /// <param name="name">The user's name.</param>
    /// <returns>The message.</returns>
    private static string StatusMessage(ChangeKind change, UpdateOutcome outcome, DisplayName name)
    {
        bool archiving = change == ChangeKind.Archived;
        return outcome switch
        {
            UpdateOutcome.Saved =>
                archiving ? $"{name.Value} archived." : $"{name.Value} restored.",
            UpdateOutcome.Unchanged =>
                archiving ? $"{name.Value} was already archived." : $"{name.Value} was already active.",
            UpdateOutcome.SelfArchive =>
                "You can't archive yourself. Another user can.",
            _ =>
                $"{name.Value} was changed by someone else at the same moment. Try again.",
        };
    }

    /// <summary>
    /// Writes the toast's message for a delete that was refused.
    /// </summary>
    /// <param name="outcome">Why it was refused.</param>
    /// <param name="name">The user's name.</param>
    /// <returns>The message.</returns>
    private static string DeleteMessage(DeleteOutcome outcome, DisplayName name)
    {
        return outcome switch
        {
            DeleteOutcome.Active =>
                $"{name.Value} is active again, so they can't be deleted.",
            DeleteOutcome.InUse =>
                $"Other records now name {name.Value}, so they can't be deleted.",
            _ =>
                $"{name.Value} has changed since the page was shown, so they weren't deleted. Check them, then try again.",
        };
    }

    /// <summary>
    /// Archives or restores the user, reads them again, and answers with what they now are and a toast that says what
    /// happened. When the user no longer exists, their page says so.
    /// </summary>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise.</returns>
    private async Task<IActionResult> ChangeStatusAsync(ChangeKind change, CancellationToken cancellationToken)
    {
        UserStatusRequest request = new UserStatusRequest
        {
            Id = new UserId(this.Input.Id),
            ChangedBy = SignedInUser.Of(this.User),
        };
        UpdateOutcome outcome = change == ChangeKind.Archived
            ? await this.Users.ArchiveAsync(request, cancellationToken).ConfigureAwait(false)
            : await this.Users.RestoreAsync(request, cancellationToken).ConfigureAwait(false);
        UserSingleResponse? account = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (account is null)
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

        return this.Answer(StatusMessage(change, outcome, account.DisplayName), undo);
    }
}
