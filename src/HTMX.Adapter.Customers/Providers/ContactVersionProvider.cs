namespace HTMX.Adapter.Customers.Providers;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The Entity Framework queries over contacts' versions: each method is one query, and what it sends is the SQL the
/// console shows. Versions are written beside their contact's row, by <see cref="ContactProvider"/>.
/// </summary>
internal sealed class ContactVersionProvider
{
    private readonly CustomersDatabaseContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactVersionProvider"/> class.
    /// </summary>
    /// <param name="context">The context of the request the queries run for.</param>
    public ContactVersionProvider(CustomersDatabaseContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this._context = context;
    }

    /// <summary>
    /// Reads every version of one contact, newest first, each with the user who made it and the organization it names,
    /// in one query.
    /// </summary>
    /// <remarks>
    /// The user is an inner join. The organization is an outer join: a version's organization is not a foreign key, so
    /// it can name an organization that has since been deleted, and the version is still read.
    /// </remarks>
    /// <param name="id">The contact's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the versions.</param>
    /// <returns>The versions, newest first.</returns>
    public Task<ContactVersionResponse[]> ListAsync(ContactId id, CancellationToken cancellationToken = default)
    {
        IQueryable<ContactVersion> versions = this._context.Set<ContactVersion>();
        IQueryable<User> users = this._context.Set<User>();
        IQueryable<Organization> organizations = this._context.Set<Organization>();
        IQueryable<ContactVersionResponse> history =
            from version in versions
            where version.ContactId == id
            join author in users on version.ChangedBy equals author.Id
            join named in organizations on version.OrganizationId equals named.Id into belongings
            from organization in belongings.DefaultIfEmpty()
            orderby version.Version descending
            select new ContactVersionResponse
            {
                Version = version.Version,
                Change = version.Change,
                RolledBackTo = version.RolledBackTo,
                Changed = new RecordChange
                {
                    On = version.ChangedOn,
                    By = new UserReference
                    {
                        Id = author.Id,
                        Name = author.DisplayName,
                        ArchivedOn = author.ArchivedOn,
                    },
                },
                Name = version.Name,
                JobTitle = version.JobTitle,
                Email = version.Email,
                PhoneNumbers = version.PhoneNumbers,
                Notes = version.Notes,
                OrganizationId = version.OrganizationId,
                Organization = organization == null
                    ? null
                    : new OrganizationReference
                    {
                        Id = organization.Id,
                        Name = organization.Name,
                        ArchivedOn = organization.ArchivedOn,
                    },
                ArchivedOn = version.ArchivedOn,
            };
        return history.ToArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Reads one version of one contact.
    /// </summary>
    /// <param name="id">The contact's identifier.</param>
    /// <param name="number">The version's number.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the version.</param>
    /// <returns>The version, or <see langword="null"/> when the contact has no such version.</returns>
    public Task<ContactVersion?> FindAsync(
        ContactId id,
        VersionNumber number,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ContactVersion> versions = this._context.Set<ContactVersion>();
        return versions
            .AsNoTracking()
            .FirstOrDefaultAsync(version => version.ContactId == id && version.Version == number, cancellationToken);
    }
}
