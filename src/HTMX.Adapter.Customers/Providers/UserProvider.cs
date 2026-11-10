namespace HTMX.Adapter.Customers.Providers;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data;
using HTMX.Adapter.Customers.DTOs;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// The Entity Framework queries over users: each method is one query, and what it sends is the SQL the console shows.
/// </summary>
internal sealed class UserProvider
{
    private readonly CustomersDatabaseContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserProvider"/> class.
    /// </summary>
    /// <param name="context">The context of the request the queries run for.</param>
    public UserProvider(CustomersDatabaseContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this._context = context;
    }

    /// <summary>
    /// Counts the users who match a filter and have a status.
    /// </summary>
    /// <param name="filter">The text the name, the username or the email address must contain, or
    /// <see langword="null"/> for every user.</param>
    /// <param name="status">Which users to count by whether they are archived.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the count.</param>
    /// <returns>The number of users.</returns>
    public async Task<RecordCount> CountAsync(
        FilterText? filter,
        RecordStatus status,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> matching = this.Match(filter, status);
        int count = await matching.CountAsync(cancellationToken).ConfigureAwait(false);
        return new RecordCount(count);
    }

    /// <summary>
    /// Reads one user with every value they hold, and the users who created, last changed and archived them, all in
    /// one query.
    /// </summary>
    /// <remarks>
    /// The users the row names are in the same table, so each is a join of the Users table to itself, as an
    /// organization's are: an inner join for the creator and the last editor, who are always set, and an outer join
    /// for the archiver, who is set only once the user is archived. Demo User created themselves, so their creator is
    /// their own row.
    /// </remarks>
    /// <param name="id">The user's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the user.</param>
    /// <returns>The user, or <see langword="null"/> when no user has the identifier.</returns>
    public Task<UserSingleResponse?> FindAsync(UserId id, CancellationToken cancellationToken = default)
    {
        IQueryable<User> users = this._context.Set<User>();
        IQueryable<UserSingleResponse> found =
            from user in users
            where user.Id == id
            join creator in users on user.CreatedBy equals creator.Id
            join editor in users on user.LastModifiedBy equals editor.Id
            join archivedBy in users on user.ArchivedBy equals (UserId?)archivedBy.Id into archivers
            from archiver in archivers.DefaultIfEmpty()
            select new UserSingleResponse
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Username = user.Username,
                Email = user.Email,
                Version = user.Version,
                Created = new RecordChange
                {
                    On = user.CreatedOn,
                    By = new UserReference
                    {
                        Id = creator.Id,
                        Name = creator.DisplayName,
                        ArchivedOn = creator.ArchivedOn,
                    },
                },
                LastModified = new RecordChange
                {
                    On = user.LastModifiedOn,
                    By = new UserReference
                    {
                        Id = editor.Id,
                        Name = editor.DisplayName,
                        ArchivedOn = editor.ArchivedOn,
                    },
                },
                Archived = user.ArchivedOn == null || archiver == null
                    ? null
                    : new RecordChange
                    {
                        On = user.ArchivedOn.Value,
                        By = new UserReference
                        {
                            Id = archiver.Id,
                            Name = archiver.DisplayName,
                            ArchivedOn = archiver.ArchivedOn,
                        },
                    },
            };
        return found.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Reads one user's row to change it. The context keeps track of the row, so a later
    /// <see cref="SaveChangeAsync"/> in the same request writes only what changed.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the row.</param>
    /// <returns>The row, or <see langword="null"/> when no user has the identifier.</returns>
    public Task<User?> FindForChangeAsync(UserId id, CancellationToken cancellationToken = default)
    {
        IQueryable<User> users = this._context.Set<User>();
        return users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    /// <summary>
    /// Says whether a user other than the one given already has a username. Usernames are compared however they are
    /// capitalized, by the column's NOCASE collation, through its unique index.
    /// </summary>
    /// <param name="username">The username to look for.</param>
    /// <param name="except">The user whose own username does not count.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns><see langword="true"/> when another user has the username.</returns>
    public Task<bool> IsUsernameTakenAsync(
        Username username,
        UserId except,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> users = this._context.Set<User>();
        return users.AnyAsync(user => user.Username == username && user.Id != except, cancellationToken);
    }

    /// <summary>
    /// Says whether a user other than the one given already has an email address. Addresses are compared however
    /// they are capitalized, by the column's NOCASE collation, through its unique index.
    /// </summary>
    /// <param name="email">The email address to look for.</param>
    /// <param name="except">The user whose own address does not count.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns><see langword="true"/> when another user has the address.</returns>
    public Task<bool> IsEmailTakenAsync(
        EmailAddress email,
        UserId except,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> users = this._context.Set<User>();
        return users.AnyAsync(user => user.Email == email && user.Id != except, cancellationToken);
    }

    /// <summary>
    /// Saves a change to a user: their row's new values, then the version that records them, in one transaction.
    /// </summary>
    /// <remarks>
    /// As for an organization, the row's version number is a concurrency token, so its UPDATE names the version that
    /// was read as well as the identifier, and a request that another saved over first changes no row and writes no
    /// version. The row is saved first, so that this check always comes before the version's insert.
    /// </remarks>
    /// <param name="current">The row as <see cref="FindForChangeAsync"/> read it.</param>
    /// <param name="changed">The row's new values.</param>
    /// <param name="version">The version that records the change.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns><see langword="true"/> when the change was saved; <see langword="false"/> when another save overtook
    /// it.</returns>
    public async Task<bool> SaveChangeAsync(
        User current,
        User changed,
        UserVersion version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(changed);
        ArgumentNullException.ThrowIfNull(version);
        DatabaseFacade database = this._context.Database;
        IDbContextTransaction transaction = await database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            EntityEntry<User> entry = this._context.Entry(current);
            PropertyValues values = entry.CurrentValues;
            values.SetValues(changed);
            bool updated = await this.TrySaveAsync(cancellationToken).ConfigureAwait(false);
            if (!updated)
            {
                return false;
            }

            this._context.Add(version);
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
    }

    /// <summary>
    /// Reads how the list sorts the user a cursor names: their value in every sortable column.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the values.</param>
    /// <returns>The values, or <see langword="null"/> when no user has the identifier.</returns>
    public Task<UserSortKey?> FindSortKeyAsync(UserId id, CancellationToken cancellationToken = default)
    {
        IQueryable<User> users = this._context.Set<User>();
        return users
            .Where(user => user.Id == id)
            .Select(static user => new UserSortKey
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Username = user.Username,
                Email = user.Email,
                LastModifiedOn = user.LastModifiedOn,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Reads users, filtered, narrowed by status and sorted, starting just past a cursor's user. A page before its
    /// cursor is read backwards from it, nearest first.
    /// </summary>
    /// <param name="request">The filter, status, sort and cursor to read.</param>
    /// <param name="cursorKey">How the list sorts the cursor's user, or <see langword="null"/> to start at the
    /// beginning.</param>
    /// <param name="limit">How many users to read at most.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the users.</param>
    /// <returns>The users, in the order they were read.</returns>
    public Task<UserSummary[]> ListAsync(
        UserCollectionRequest request,
        UserSortKey? cursorKey,
        RecordCount limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        IQueryable<User> matching = this.Match(request.Filter, request.Status);
        SortDirection direction = ReadDirection(request);
        if (cursorKey is not null)
        {
            matching = matching.Where(UserSeek.Beyond(cursorKey, request.Sort, direction));
        }

        return Sort(matching, request.Sort, direction)
            .Take(limit.Value)
            .Select(static user => new UserSummary
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Username = user.Username,
                Email = user.Email,
                LastModifiedOn = user.LastModifiedOn,
                ArchivedOn = user.ArchivedOn,
            })
            .ToArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Says which way to read: the list's own order, or its reverse for a page before the cursor.
    /// </summary>
    /// <param name="request">The request, with the list's order and the cursor's position.</param>
    /// <returns>The direction to read in.</returns>
    private static SortDirection ReadDirection(UserCollectionRequest request)
    {
        if (request.Cursor?.Position != CursorPosition.Before)
        {
            return request.Direction;
        }

        return request.Direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
    }

    /// <summary>
    /// Sorts users by a column, then by identifier in the same direction, so that ties keep one order from page to page
    /// and an index on the column and the identifier can give the whole order.
    /// </summary>
    /// <param name="users">The users to sort.</param>
    /// <param name="column">What to sort by.</param>
    /// <param name="direction">Which way to sort.</param>
    /// <returns>The sorted users.</returns>
    private static IOrderedQueryable<User> Sort(
        IQueryable<User> users,
        UserSortColumn column,
        SortDirection direction)
    {
        IOrderedQueryable<User> sorted = column switch
        {
            UserSortColumn.Username =>
                OrderBy(users, static user => user.Username, direction),
            UserSortColumn.Email =>
                OrderBy(users, static user => user.Email, direction),
            UserSortColumn.Updated =>
                OrderBy(users, static user => user.LastModifiedOn, direction),
            _ =>
                OrderBy(users, static user => user.DisplayName, direction),
        };
        return direction == SortDirection.Descending
            ? sorted.ThenByDescending(static user => user.Id)
            : sorted.ThenBy(static user => user.Id);
    }

    /// <summary>
    /// Orders users by one key in one direction.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="users">The users to order.</param>
    /// <param name="key">The key to order by.</param>
    /// <param name="direction">Which way to order.</param>
    /// <returns>The ordered users.</returns>
    private static IOrderedQueryable<User> OrderBy<TKey>(
        IQueryable<User> users,
        Expression<Func<User, TKey>> key,
        SortDirection direction)
    {
        return direction == SortDirection.Descending
            ? users.OrderByDescending(key)
            : users.OrderBy(key);
    }

    /// <summary>
    /// Narrows the users to those whose name, username or email address contains the filter, and whose status
    /// matches.
    /// </summary>
    /// <param name="filter">The text to find, or <see langword="null"/> for every user.</param>
    /// <param name="status">Which users to keep by whether they are archived.</param>
    /// <returns>The query for the matching users.</returns>
    private IQueryable<User> Match(FilterText? filter, RecordStatus status)
    {
        IQueryable<User> users = this._context.Set<User>();
        if (filter is FilterText text)
        {
            // SQLite's LIKE ignores the case of A to Z. The casts let LIKE see each value as the text it is stored as.
            string pattern = LikePattern.Contains(text);
            users = users.Where(user =>
                EF.Functions.Like((string)(object)user.DisplayName, pattern, LikePattern.Escape)
                || EF.Functions.Like((string)(object)user.Username, pattern, LikePattern.Escape)
                || EF.Functions.Like((string)(object)user.Email, pattern, LikePattern.Escape));
        }

        return status switch
        {
            RecordStatus.Active =>
                users.Where(static user => user.ArchivedOn == null),
            RecordStatus.Archived =>
                users.Where(static user => user.ArchivedOn != null),
            _ =>
                users,
        };
    }

    /// <summary>
    /// Saves what the context holds, unless a row it changes was saved by another request since it was read.
    /// </summary>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns><see langword="false"/> when a concurrency token no longer matched.</returns>
    private async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // An expected outcome, not a failure: the caller tells the user, who decides whether to try again.
            return false;
        }
    }
}
