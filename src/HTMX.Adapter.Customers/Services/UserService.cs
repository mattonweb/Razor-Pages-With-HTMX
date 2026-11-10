namespace HTMX.Adapter.Customers.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.DTOs;
using HTMX.Adapter.Customers.Entities;
using HTMX.Adapter.Customers.Providers;

/// <summary>
/// The rules over users, behind <see cref="IUserService"/>; the queries themselves are the providers'.
/// </summary>
internal sealed class UserService : IUserService
{
    private readonly UserProvider _provider;
    private readonly UserVersionProvider _versions;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="provider">Runs the queries over users.</param>
    /// <param name="versions">Runs the queries over their versions.</param>
    /// <param name="clock">Says when a change is made.</param>
    public UserService(UserProvider provider, UserVersionProvider versions, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(versions);
        ArgumentNullException.ThrowIfNull(clock);
        this._provider = provider;
        this._versions = versions;
        this._clock = clock;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// As for the organizations list, a page is read one user longer than it shows: if that extra user is there, so is
    /// another page, and no count is needed to know it. A cursor whose user no longer exists, or that leaves too little
    /// to show, starts again at the first page, so an old link never shows an empty or a short page.
    /// </remarks>
    public async Task<UserCollectionResponse> ListAsync(
        UserCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RecordCount active = await this._provider
            .CountAsync(request.Filter, RecordStatus.Active, cancellationToken)
            .ConfigureAwait(false);
        RecordCount archived = await this._provider
            .CountAsync(request.Filter, RecordStatus.Archived, cancellationToken)
            .ConfigureAwait(false);

        UserSortKey? key = await this.FindCursorKeyAsync(request.Cursor, cancellationToken).ConfigureAwait(false);
        UserCollectionRequest answered = key is null ? UserPages.FirstPage(request) : request;
        UserSummary[] read = await this.ReadAsync(answered, key, cancellationToken).ConfigureAwait(false);
        if (key is not null && UserPages.IsTooShort(answered, read))
        {
            answered = UserPages.FirstPage(request);
            read = await this.ReadAsync(answered, null, cancellationToken).ConfigureAwait(false);
        }

        return UserPages.CreatePage(answered, read, active, archived);
    }

    /// <inheritdoc/>
    public Task<UserSingleResponse?> FindAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return this._provider.FindAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserVersionResponse[]> ListVersionsAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return this._versions.ListAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The rules run in order, and the first that applies is the answer: the earlier version and the user must exist,
    /// the user must be active, still at the version the request names, and hold some value other than the earlier
    /// version's, and no other user may have its username or email address now, however it is capitalized. A roll-back
    /// that passes is saved as a RolledBack version that names the version it went back to, and the row takes its
    /// values, as the seed builds every row from its latest version. History is never rewritten: rolling back adds a
    /// version, and rolling back again undoes it.
    /// </remarks>
    public async Task<UpdateOutcome> RollBackAsync(
        UserRollBackRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        UserVersion? earlier = await this._versions
            .FindAsync(request.Id, request.To, cancellationToken)
            .ConfigureAwait(false);
        User? current = await this._provider.FindForChangeAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (earlier is null || current is null)
        {
            return UpdateOutcome.NotFound;
        }

        if (OutcomeWithoutSaving(current, request.Version, earlier) is UpdateOutcome settled)
        {
            return settled;
        }

        if (await this.FindClashAsync(earlier, cancellationToken).ConfigureAwait(false) is UpdateOutcome clash)
        {
            return clash;
        }

        UserVersion version = RecordRollBack(current, earlier, request, this._clock.GetUtcNow().UtcDateTime);
        User changed = Apply(current, version);
        bool saved = await this._provider
            .SaveChangeAsync(current, changed, version, cancellationToken)
            .ConfigureAwait(false);
        return saved ? UpdateOutcome.Saved : UpdateOutcome.Overtaken;
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> ArchiveAsync(UserStatusRequest request, CancellationToken cancellationToken = default)
    {
        return this.ChangeStatusAsync(request, ChangeKind.Archived, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> RestoreAsync(UserStatusRequest request, CancellationToken cancellationToken = default)
    {
        return this.ChangeStatusAsync(request, ChangeKind.Restored, cancellationToken);
    }

    /// <summary>
    /// Gives the outcome a roll-back has before anything is saved: refused because the user is archived or has moved
    /// on since their history was shown, or done already because they hold the earlier version's values.
    /// </summary>
    /// <param name="current">The user as they are now.</param>
    /// <param name="version">The version the request says the user is at.</param>
    /// <param name="earlier">The version to roll back to.</param>
    /// <returns>The outcome, or <see langword="null"/> when the roll-back can go on to be saved.</returns>
    private static UpdateOutcome? OutcomeWithoutSaving(User current, VersionNumber version, UserVersion earlier)
    {
        if (current.ArchivedOn is not null)
        {
            return UpdateOutcome.Archived;
        }

        if (current.Version != version)
        {
            return UpdateOutcome.Overtaken;
        }

        bool same = current.DisplayName == earlier.DisplayName
            && current.Username == earlier.Username
            && current.Email == earlier.Email;
        return same ? UpdateOutcome.Unchanged : null;
    }

    /// <summary>
    /// Makes the version that records a roll-back: the next number, the earlier version's values, and who saved them
    /// and when.
    /// </summary>
    /// <param name="current">The user as they are now, who is active.</param>
    /// <param name="earlier">The version whose values are saved again.</param>
    /// <param name="request">The roll-back, with who asked for it.</param>
    /// <param name="now">When the roll-back is saved, in UTC.</param>
    /// <returns>The version.</returns>
    private static UserVersion RecordRollBack(
        User current,
        UserVersion earlier,
        UserRollBackRequest request,
        DateTime now)
    {
        return new UserVersion
        {
            UserId = current.Id,
            Version = new VersionNumber(current.Version.Value + 1),
            Change = ChangeKind.RolledBack,
            RolledBackTo = earlier.Version,
            ChangedOn = now,
            ChangedBy = request.ChangedBy,
            Username = earlier.Username,
            Email = earlier.Email,
            DisplayName = earlier.DisplayName,
        };
    }

    /// <summary>
    /// Makes the version that records a user being archived or restored: the next number, their values as they are,
    /// and when and by whom they were archived, or nothing there once they are restored.
    /// </summary>
    /// <param name="current">The user as they are now.</param>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="by">Who is archiving or restoring them.</param>
    /// <param name="now">When, in UTC.</param>
    /// <returns>The version.</returns>
    private static UserVersion RecordStatusChange(User current, ChangeKind change, UserId by, DateTime now)
    {
        bool archiving = change == ChangeKind.Archived;
        return new UserVersion
        {
            UserId = current.Id,
            Version = new VersionNumber(current.Version.Value + 1),
            Change = change,
            ChangedOn = now,
            ChangedBy = by,
            Username = current.Username,
            Email = current.Email,
            DisplayName = current.DisplayName,
            ArchivedOn = archiving ? now : null,
            ArchivedBy = archiving ? by : null,
        };
    }

    /// <summary>
    /// Gives a user's row the values of their newest version, keeping who created them and when.
    /// </summary>
    /// <param name="current">The row as it is now.</param>
    /// <param name="version">The newest version.</param>
    /// <returns>The row's new values.</returns>
    private static User Apply(User current, UserVersion version)
    {
        // Written out rather than with a with expression, which StyleCop 1.1.118 misreads in an instance class (SA1101).
        return new User
        {
            Id = current.Id,
            Username = version.Username,
            Email = version.Email,
            DisplayName = version.DisplayName,
            CreatedOn = current.CreatedOn,
            CreatedBy = current.CreatedBy,
            LastModifiedOn = version.ChangedOn,
            LastModifiedBy = version.ChangedBy,
            ArchivedOn = version.ArchivedOn,
            ArchivedBy = version.ArchivedBy,
            Version = version.Version,
        };
    }

    /// <summary>
    /// Says whether another user now has the username or the email address an earlier version holds, either of which
    /// would stop a roll-back to it.
    /// </summary>
    /// <param name="earlier">The version to roll back to.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>NameTaken for the username, EmailTaken for the email address, or <see langword="null"/> when neither
    /// is taken.</returns>
    private async Task<UpdateOutcome?> FindClashAsync(UserVersion earlier, CancellationToken cancellationToken)
    {
        bool usernameTaken = await this._provider
            .IsUsernameTakenAsync(earlier.Username, earlier.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (usernameTaken)
        {
            return UpdateOutcome.NameTaken;
        }

        bool emailTaken = await this._provider
            .IsEmailTakenAsync(earlier.Email, earlier.UserId, cancellationToken)
            .ConfigureAwait(false);
        return emailTaken ? UpdateOutcome.EmailTaken : null;
    }

    /// <summary>
    /// Reads how the list sorts the user a cursor names.
    /// </summary>
    /// <param name="cursor">The cursor, if any.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the values.</param>
    /// <returns>The values, or <see langword="null"/> for no cursor or a user who no longer exists.</returns>
    private async Task<UserSortKey?> FindCursorKeyAsync(UserCursor? cursor, CancellationToken cancellationToken)
    {
        if (cursor is not UserCursor named)
        {
            return null;
        }

        return await this._provider.FindSortKeyAsync(named.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads one user more than a page holds, starting just past the cursor's user.
    /// </summary>
    /// <param name="request">The page to read.</param>
    /// <param name="key">How the list sorts the cursor's user, or <see langword="null"/> for the first page.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the users.</param>
    /// <returns>The users, in the order they were read.</returns>
    private Task<UserSummary[]> ReadAsync(
        UserCollectionRequest request,
        UserSortKey? key,
        CancellationToken cancellationToken)
    {
        RecordCount limit = new RecordCount(request.PageSize.Value + 1);
        return this._provider.ListAsync(request, key, limit, cancellationToken);
    }

    /// <summary>
    /// Archives or restores a user as their next version. Nobody may archive themselves, since they would be shut out;
    /// a user already archived, or already active, is left as they are.
    /// </summary>
    /// <param name="request">Which user, and who is asking.</param>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>What became of the request.</returns>
    private async Task<UpdateOutcome> ChangeStatusAsync(
        UserStatusRequest request,
        ChangeKind change,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        bool archiving = change == ChangeKind.Archived;
        if (archiving && request.Id == request.ChangedBy)
        {
            return UpdateOutcome.SelfArchive;
        }

        User? current = await this._provider.FindForChangeAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return UpdateOutcome.NotFound;
        }

        if ((current.ArchivedOn is not null) == archiving)
        {
            return UpdateOutcome.Unchanged;
        }

        DateTime now = this._clock.GetUtcNow().UtcDateTime;
        UserVersion version = RecordStatusChange(current, change, request.ChangedBy, now);
        User changed = Apply(current, version);
        bool saved = await this._provider
            .SaveChangeAsync(current, changed, version, cancellationToken)
            .ConfigureAwait(false);
        return saved ? UpdateOutcome.Saved : UpdateOutcome.Overtaken;
    }
}
