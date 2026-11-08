namespace HTMX.Adapter.Contacts.Data.Seed;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Contacts.Contracts;
using HTMX.Adapter.Contacts.Entities;

/// <summary>
/// Writes the seed into a new database: every version, moved forward so the seed ends now, and each user's current
/// row built from their latest version.
/// </summary>
internal sealed class SeedWriter
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeedWriter"/> class.
    /// </summary>
    /// <param name="timeProvider">The clock that says what now is.</param>
    public SeedWriter(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        this._timeProvider = timeProvider;
    }

    /// <summary>
    /// Writes every seed user and version, in one save.
    /// </summary>
    /// <param name="context">A context on the new, empty database.</param>
    /// <param name="cancellationToken">Signals that the application is stopping before it has started.</param>
    /// <returns>A task that completes when the seed is saved.</returns>
    public async Task WriteAsync(ContactsDatabaseContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        DateTimeOffset now = this._timeProvider.GetUtcNow();
        TimeSpan sinceWritten = now.UtcDateTime - UserSeed.WrittenOn;

        UserVersion[] versions =
        [
            .. UserSeed.Versions
                .Concat(FillerUserSeed.Versions)
                .Select(version => MoveForward(version, sinceWritten)),
        ];
        User[] users = [.. versions.GroupBy(static version => version.UserId).Select(BuildUser)];

        context.Set<User>().AddRange(users);
        context.Set<UserVersion>().AddRange(versions);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Moves a version's dates forward by the time since the seed was written.
    /// </summary>
    /// <param name="version">The version as the seed writes it.</param>
    /// <param name="sinceWritten">The time since the seed was written.</param>
    /// <returns>The same version, with its dates moved.</returns>
    private static UserVersion MoveForward(UserVersion version, TimeSpan sinceWritten)
    {
        return version with
        {
            ChangedOn = version.ChangedOn + sinceWritten,
            ArchivedOn = version.ArchivedOn + sinceWritten,
        };
    }

    /// <summary>
    /// Builds a user's current row from their versions: their values, archive state and number from the latest, and
    /// their creation from the first.
    /// </summary>
    /// <param name="history">Every version of one user.</param>
    /// <returns>The user's row.</returns>
    private static User BuildUser(IGrouping<UserId, UserVersion> history)
    {
        UserVersion[] versions = [.. history.OrderBy(static version => version.Version.Value)];
        UserVersion first = versions[0];
        UserVersion latest = versions[versions.Length - 1];
        return new User
        {
            Id = latest.UserId,
            Username = latest.Username,
            Email = latest.Email,
            DisplayName = latest.DisplayName,
            CreatedOn = first.ChangedOn,
            CreatedBy = first.ChangedBy,
            LastModifiedOn = latest.ChangedOn,
            LastModifiedBy = latest.ChangedBy,
            ArchivedOn = latest.ArchivedOn,
            ArchivedBy = latest.ArchivedBy,
            Version = latest.Version,
        };
    }
}
