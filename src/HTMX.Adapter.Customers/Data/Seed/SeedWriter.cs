namespace HTMX.Adapter.Customers.Data.Seed;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// Writes the seed into a new database: every version, dated back from now, and each user's and organization's
/// current row built from its latest version.
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
    /// Writes every seed user and organization, with their versions, in one save.
    /// </summary>
    /// <param name="context">A context on the new, empty database.</param>
    /// <param name="cancellationToken">Signals that the application is stopping before it has started.</param>
    /// <returns>A task that completes when the seed is saved.</returns>
    public async Task WriteAsync(CustomersDatabaseContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        DateTimeOffset now = this._timeProvider.GetUtcNow();
        DateTime startedOn = now.UtcDateTime;

        UserVersion[] userVersions =
        [
            .. UserSeed.CreateVersions(startedOn),
            .. FillerUserSeed.CreateVersions(startedOn),
        ];
        User[] users = [.. userVersions.GroupBy(static version => version.UserId).Select(BuildUser)];
        context.Set<User>().AddRange(users);
        context.Set<UserVersion>().AddRange(userVersions);

        OrganizationVersion[] organizationVersions =
        [
            .. OrganizationSeed.CreateVersions(startedOn),
            .. GeneratedOrganizationSeed.CreateVersions(startedOn),
        ];
        Organization[] organizations =
        [
            .. organizationVersions.GroupBy(static version => version.OrganizationId).Select(BuildOrganization),
        ];
        context.Set<Organization>().AddRange(organizations);
        context.Set<OrganizationVersion>().AddRange(organizationVersions);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Builds an organization's current row from its versions, as <see cref="BuildUser"/> does for a user.
    /// </summary>
    /// <param name="history">Every version of one organization.</param>
    /// <returns>The organization's row.</returns>
    private static Organization BuildOrganization(IGrouping<OrganizationId, OrganizationVersion> history)
    {
        OrganizationVersion[] versions = [.. history.OrderBy(static version => version.Version.Value)];
        OrganizationVersion first = versions[0];
        OrganizationVersion latest = versions[versions.Length - 1];
        return new Organization
        {
            Id = latest.OrganizationId,
            Name = latest.Name,
            Industry = latest.Industry,
            Website = latest.Website,
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
