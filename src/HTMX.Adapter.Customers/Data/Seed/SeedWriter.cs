namespace HTMX.Adapter.Customers.Data.Seed;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// Writes the seed into a new database: every version, dated back from now, and each user's, organization's and
/// contact's current row built from its latest version.
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
    /// Writes every seed user, organization and contact, with their versions, in one save.
    /// </summary>
    /// <param name="context">A context on the new, empty database.</param>
    /// <param name="cancellationToken">Signals that the application is stopping before it has started.</param>
    /// <returns>A task that completes when the seed is saved.</returns>
    public async Task WriteAsync(CustomersDatabaseContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        DateTimeOffset now = this._timeProvider.GetUtcNow();
        DateTime startedOn = now.UtcDateTime;

        AddUsers(context, startedOn);
        Organization[] organizations = AddOrganizations(context, startedOn);
        AddContacts(context, startedOn, organizations);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds the seed users and their versions to the context.
    /// </summary>
    /// <param name="context">The context the seed is written through.</param>
    /// <param name="startedOn">When the application started.</param>
    private static void AddUsers(CustomersDatabaseContext context, DateTime startedOn)
    {
        UserVersion[] versions =
        [
            .. UserSeed.CreateVersions(startedOn),
            .. FillerUserSeed.CreateVersions(startedOn),
        ];
        User[] users = [.. versions.GroupBy(static version => version.UserId).Select(BuildUser)];
        context.Set<User>().AddRange(users);
        context.Set<UserVersion>().AddRange(versions);
    }

    /// <summary>
    /// Adds the seed organizations and their versions to the context.
    /// </summary>
    /// <param name="context">The context the seed is written through.</param>
    /// <param name="startedOn">When the application started.</param>
    /// <returns>The organizations' rows, which the generated contacts are placed in.</returns>
    private static Organization[] AddOrganizations(CustomersDatabaseContext context, DateTime startedOn)
    {
        OrganizationVersion[] versions =
        [
            .. OrganizationSeed.CreateVersions(startedOn),
            .. GeneratedOrganizationSeed.CreateVersions(startedOn),
        ];
        Organization[] organizations =
        [
            .. versions.GroupBy(static version => version.OrganizationId).Select(BuildOrganization),
        ];
        context.Set<Organization>().AddRange(organizations);
        context.Set<OrganizationVersion>().AddRange(versions);
        return organizations;
    }

    /// <summary>
    /// Adds the seed contacts and their versions to the context.
    /// </summary>
    /// <param name="context">The context the seed is written through.</param>
    /// <param name="startedOn">When the application started.</param>
    /// <param name="organizations">The organizations' rows, which the generated contacts are placed in.</param>
    private static void AddContacts(CustomersDatabaseContext context, DateTime startedOn, Organization[] organizations)
    {
        ContactVersion[] versions =
        [
            .. ContactSeed.CreateVersions(startedOn),
            .. GeneratedContactSeed.CreateVersions(startedOn, organizations),
        ];
        Contact[] contacts = [.. versions.GroupBy(static version => version.ContactId).Select(BuildContact)];
        context.Set<Contact>().AddRange(contacts);
        context.Set<ContactVersion>().AddRange(versions);
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

    /// <summary>
    /// Builds a contact's current row from their versions, as <see cref="BuildUser"/> does for a user.
    /// </summary>
    /// <param name="history">Every version of one contact.</param>
    /// <returns>The contact's row.</returns>
    private static Contact BuildContact(IGrouping<ContactId, ContactVersion> history)
    {
        ContactVersion[] versions = [.. history.OrderBy(static version => version.Version.Value)];
        ContactVersion first = versions[0];
        ContactVersion latest = versions[versions.Length - 1];
        return new Contact
        {
            Id = latest.ContactId,
            Name = latest.Name,
            Email = latest.Email,
            Phone = latest.Phone,
            JobTitle = latest.JobTitle,
            OrganizationId = latest.OrganizationId,
            Notes = latest.Notes,
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
