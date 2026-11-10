namespace HTMX.Application.Web.Pages.Users;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// The History tab of a user's page, at /users/{id}/history: every version, newest first, with what each changed, and
/// a roll-back to any earlier one. Rolling back saves the earlier values as a new version, so it says so in a toast
/// whose Undo rolls back again, to the version it replaced.
/// </summary>
public sealed class HistoryModel : UserPageModel
{
    private const string RollBackHandler = "RollBack";

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryModel"/> class.
    /// </summary>
    /// <param name="users">Reads the user and their versions, and rolls them back.</param>
    public HistoryModel(IUserService users)
        : base(users)
    {
    }

    /// <inheritdoc/>
    public override string PageName => "./History";

    /// <inheritdoc/>
    public override string ContentPartial => "_UserHistory";

    /// <summary>
    /// Gets the user's versions, newest first.
    /// </summary>
    public IReadOnlyList<UserVersionResponse> Versions { get; private set; } = [];

    /// <summary>
    /// Gets the versions as the timeline shows them, or <see langword="null"/> until they are read.
    /// </summary>
    public Timeline? Timeline { get; private set; }

    /// <summary>
    /// Rolls the user back to an earlier version, from the version the page showed them at.
    /// </summary>
    /// <param name="to">The version to roll back to, from the preview's form.</param>
    /// <param name="version">The version the page showed the user at, from the preview's form.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise; 400 for a version
    /// number that cannot be one.</returns>
    public async Task<IActionResult> OnPostRollBackAsync(int to, int version, CancellationToken cancellationToken)
    {
        if (to < 1 || version < 1)
        {
            return this.BadRequest();
        }

        UserRollBackRequest request = new UserRollBackRequest
        {
            Id = new UserId(this.Input.Id),
            Version = new VersionNumber(version),
            To = new VersionNumber(to),
            ChangedBy = SignedInUser.Of(this.User),
        };
        UpdateOutcome outcome = await this.Users.RollBackAsync(request, cancellationToken).ConfigureAwait(false);
        UserSingleResponse? account = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return this.ShowGone();
        }

        // Undo rolls back again, to the version this one replaced, from the version the roll-back made.
        string? undo = outcome == UpdateOutcome.Saved
            ? this.HandlerAddress(RollBackHandler, new { id = this.Input.Id, to = version, version = version + 1 })
            : null;
        return this.Answer(this.RollBackMessage(outcome, to, account.DisplayName), undo);
    }

    /// <inheritdoc/>
    protected override async Task ReadMoreAsync(UserId id, CancellationToken cancellationToken)
    {
        this.Versions = await this.Users.ListVersionsAsync(id, cancellationToken).ConfigureAwait(false);
        UserSingleResponse account = this.Account
            ?? throw new InvalidOperationException("The versions are read after the user.");
        this.Timeline = new Timeline
        {
            RecordName = account.DisplayName.Value,
            Current = account.Version,
            Entries = UserChanges.Entries(account, this.Versions),
            RollBackAction = this.HandlerAddress(RollBackHandler, new { id = this.Input.Id }),
            Target = this.ContentTarget,
        };
    }

    /// <summary>
    /// Writes the toast's message for what became of a roll-back.
    /// </summary>
    /// <param name="outcome">What became of it.</param>
    /// <param name="to">The version it was to.</param>
    /// <param name="name">The user's name now.</param>
    /// <returns>The message.</returns>
    private string RollBackMessage(UpdateOutcome outcome, int to, DisplayName name)
    {
        return outcome switch
        {
            UpdateOutcome.Saved =>
                $"{name.Value} rolled back to version {to}.",
            UpdateOutcome.Unchanged =>
                $"{name.Value} already holds the values of version {to}.",
            UpdateOutcome.Archived =>
                $"{name.Value} is archived, so they can't be rolled back until they are restored.",
            UpdateOutcome.NameTaken =>
                $"Another user now has {this.UsernameIn(to)}, so {name.Value} can't go back to version {to}.",
            UpdateOutcome.EmailTaken =>
                $"Another user now has {this.EmailIn(to)}, so {name.Value} can't go back to version {to}.",
            UpdateOutcome.NotFound =>
                $"{name.Value} has no version {to}.",
            _ =>
                $"{name.Value} has changed since their history was shown. Check the newest version, then try again.",
        };
    }

    /// <summary>
    /// Names the username a user held at one of their versions, as a refused roll-back's message does.
    /// </summary>
    /// <param name="number">The version's number.</param>
    /// <returns>"the username" and the username, or "that username" when the version is not among those read.</returns>
    private string UsernameIn(int number)
    {
        UserVersionResponse? version = this.Versions.FirstOrDefault(version => version.Version.Value == number);
        return version is null ? "that username" : $"the username {version.Username.Value}";
    }

    /// <summary>
    /// Names the email address a user held at one of their versions, as a refused roll-back's message does.
    /// </summary>
    /// <param name="number">The version's number.</param>
    /// <returns>"the email address" and the address, or "that email address" when the version is not among those
    /// read.</returns>
    private string EmailIn(int number)
    {
        UserVersionResponse? version = this.Versions.FirstOrDefault(version => version.Version.Value == number);
        return version is null ? "that email address" : $"the email address {version.Email.Value}";
    }
}
