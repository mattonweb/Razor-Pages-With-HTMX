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
/// The Entity Framework queries over users' versions: each method is one query, and what it sends is the SQL the
/// console shows. Versions are written beside their user's row, by <see cref="UserProvider"/>.
/// </summary>
internal sealed class UserVersionProvider
{
    private readonly CustomersDatabaseContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserVersionProvider"/> class.
    /// </summary>
    /// <param name="context">The context of the request the queries run for.</param>
    public UserVersionProvider(CustomersDatabaseContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this._context = context;
    }

    /// <summary>
    /// Reads every version of one user, newest first, each with the user who made it, in one query that joins the
    /// Users table to itself.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the versions.</param>
    /// <returns>The versions, newest first.</returns>
    public Task<UserVersionResponse[]> ListAsync(UserId id, CancellationToken cancellationToken = default)
    {
        IQueryable<UserVersion> versions = this._context.Set<UserVersion>();
        IQueryable<User> users = this._context.Set<User>();
        IQueryable<UserVersionResponse> history =
            from version in versions
            where version.UserId == id
            join author in users on version.ChangedBy equals author.Id
            orderby version.Version descending
            select new UserVersionResponse
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
                DisplayName = version.DisplayName,
                Username = version.Username,
                Email = version.Email,
                ArchivedOn = version.ArchivedOn,
            };
        return history.ToArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Reads one version of one user.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="number">The version's number.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the version.</param>
    /// <returns>The version, or <see langword="null"/> when the user has no such version.</returns>
    public Task<UserVersion?> FindAsync(
        UserId id,
        VersionNumber number,
        CancellationToken cancellationToken = default)
    {
        IQueryable<UserVersion> versions = this._context.Set<UserVersion>();
        return versions
            .AsNoTracking()
            .FirstOrDefaultAsync(version => version.UserId == id && version.Version == number, cancellationToken);
    }
}
