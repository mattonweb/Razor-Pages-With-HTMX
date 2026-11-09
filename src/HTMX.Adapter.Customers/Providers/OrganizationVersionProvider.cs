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
/// The Entity Framework queries over organizations' versions: each method is one query, and what it sends is the SQL
/// the console shows. Versions are written beside their organization's row, by <see cref="OrganizationProvider"/>.
/// </summary>
internal sealed class OrganizationVersionProvider
{
    private readonly CustomersDatabaseContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationVersionProvider"/> class.
    /// </summary>
    /// <param name="context">The context of the request the queries run for.</param>
    public OrganizationVersionProvider(CustomersDatabaseContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this._context = context;
    }

    /// <summary>
    /// Reads every version of one organization, newest first, each with the user who made it, in one query that joins
    /// the Users table.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the versions.</param>
    /// <returns>The versions, newest first.</returns>
    public Task<OrganizationVersionResponse[]> ListAsync(
        OrganizationId id,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OrganizationVersion> versions = this._context.Set<OrganizationVersion>();
        IQueryable<User> users = this._context.Set<User>();
        IQueryable<OrganizationVersionResponse> history =
            from version in versions
            where version.OrganizationId == id
            join author in users on version.ChangedBy equals author.Id
            orderby version.Version descending
            select new OrganizationVersionResponse
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
                Industry = version.Industry,
                Website = version.Website,
                ArchivedOn = version.ArchivedOn,
            };
        return history.ToArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Reads one version of one organization.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="number">The version's number.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the version.</param>
    /// <returns>The version, or <see langword="null"/> when the organization has no such version.</returns>
    public Task<OrganizationVersion?> FindAsync(
        OrganizationId id,
        VersionNumber number,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OrganizationVersion> versions = this._context.Set<OrganizationVersion>();
        return versions
            .AsNoTracking()
            .FirstOrDefaultAsync(version => version.OrganizationId == id && version.Version == number, cancellationToken);
    }
}
