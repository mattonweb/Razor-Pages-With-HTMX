namespace HTMX.Adapter.Customers.Providers;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data;
using HTMX.Adapter.Customers.Data.Conversions;
using HTMX.Adapter.Customers.DTOs;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// The Entity Framework queries over organizations: each method is one query, and what it sends is the SQL the console
/// shows.
/// </summary>
internal sealed class OrganizationProvider
{
    // The character that makes the next % or _ in a LIKE pattern literal, so a filter of "50%" means fifty percent.
    private const string LikeEscape = "\\";

    private readonly CustomersDatabaseContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationProvider"/> class.
    /// </summary>
    /// <param name="context">The context of the request the queries run for.</param>
    public OrganizationProvider(CustomersDatabaseContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this._context = context;
    }

    /// <summary>
    /// Counts the organizations that match a filter and have a status.
    /// </summary>
    /// <param name="filter">The text the names must contain, or <see langword="null"/> for every organization.</param>
    /// <param name="status">Which organizations to count by whether they are archived.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the count.</param>
    /// <returns>The number of organizations.</returns>
    public async Task<RecordCount> CountAsync(
        FilterText? filter,
        RecordStatus status,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Organization> matching = this.Match(filter, status);
        int count = await matching.CountAsync(cancellationToken).ConfigureAwait(false);
        return new RecordCount(count);
    }

    /// <summary>
    /// Reads one organization with every value it holds, its number of contacts, and the users who created, last
    /// changed and archived it, all in one query.
    /// </summary>
    /// <remarks>
    /// Each user the organization names is a join on the Users table: an inner join for the creator and the last
    /// editor, who are always set, and an outer join for the archiver, who is set only once the organization is
    /// archived. A subquery that picks the first matching user would read the same, but becomes a window function over
    /// every user in the SQL.
    /// </remarks>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the organization.</param>
    /// <returns>The organization, or <see langword="null"/> when no organization has the identifier.</returns>
    public Task<OrganizationSingleResponse?> FindAsync(
        OrganizationId id,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Contact> contacts = this._context.Set<Contact>();
        IQueryable<User> users = this._context.Set<User>();
        IQueryable<Organization> organizations = this._context.Set<Organization>();
        IQueryable<OrganizationSingleResponse> found =
            from organization in organizations
            where organization.Id == id
            join creator in users on organization.CreatedBy equals creator.Id
            join editor in users on organization.LastModifiedBy equals editor.Id
            join archivedBy in users on organization.ArchivedBy equals (UserId?)archivedBy.Id into archivers
            from archiver in archivers.DefaultIfEmpty()
            select new OrganizationSingleResponse
            {
                Id = organization.Id,
                Name = organization.Name,
                Industry = organization.Industry,
                Website = organization.Website,
                Contacts = new RecordCount(contacts.Count(contact => contact.OrganizationId == organization.Id)),
                Version = organization.Version,
                Created = new RecordChange
                {
                    On = organization.CreatedOn,
                    By = new UserReference
                    {
                        Id = creator.Id,
                        Name = creator.DisplayName,
                        ArchivedOn = creator.ArchivedOn,
                    },
                },
                LastModified = new RecordChange
                {
                    On = organization.LastModifiedOn,
                    By = new UserReference
                    {
                        Id = editor.Id,
                        Name = editor.DisplayName,
                        ArchivedOn = editor.ArchivedOn,
                    },
                },
                Archived = organization.ArchivedOn == null || archiver == null
                    ? null
                    : new RecordChange
                    {
                        On = organization.ArchivedOn.Value,
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
    /// Reads how the list sorts the organization a cursor names: its value in every sortable column.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the values.</param>
    /// <returns>The values, or <see langword="null"/> when no organization has the identifier.</returns>
    public Task<OrganizationSortKey?> FindSortKeyAsync(OrganizationId id, CancellationToken cancellationToken = default)
    {
        IQueryable<Contact> contacts = this._context.Set<Contact>();
        IQueryable<Organization> organizations = this._context.Set<Organization>();
        return organizations
            .Where(organization => organization.Id == id)
            .Select(organization => new OrganizationSortKey
            {
                Id = organization.Id,
                Name = organization.Name,
                Industry = organization.Industry,
                Contacts = new RecordCount(contacts.Count(contact => contact.OrganizationId == organization.Id)),
                LastModifiedOn = organization.LastModifiedOn,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Reads one organization's row to change it. The context keeps track of the row, so a later
    /// <see cref="SaveChangeAsync"/> in the same request writes only what changed.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the row.</param>
    /// <returns>The row, or <see langword="null"/> when no organization has the identifier.</returns>
    public Task<Organization?> FindForChangeAsync(OrganizationId id, CancellationToken cancellationToken = default)
    {
        IQueryable<Organization> organizations = this._context.Set<Organization>();
        return organizations.FirstOrDefaultAsync(organization => organization.Id == id, cancellationToken);
    }

    /// <summary>
    /// Says whether an organization other than the one given already has a name. Names are compared however they are
    /// capitalized, by the column's NOCASE collation, through its unique index.
    /// </summary>
    /// <param name="name">The name to look for.</param>
    /// <param name="except">The organization whose own name does not count.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns><see langword="true"/> when another organization has the name.</returns>
    public Task<bool> IsNameTakenAsync(
        OrganizationName name,
        OrganizationId except,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Organization> organizations = this._context.Set<Organization>();
        return organizations.AnyAsync(
            organization => organization.Name == name && organization.Id != except,
            cancellationToken);
    }

    /// <summary>
    /// Saves a change to an organization: its row's new values, then the version that records them, in one
    /// transaction.
    /// </summary>
    /// <remarks>
    /// The row's version number is a concurrency token, so its UPDATE names the version that was read as well as the
    /// identifier. If another request saved the organization after this one read it, the UPDATE matches no row,
    /// Entity Framework raises <see cref="DbUpdateConcurrencyException"/>, and the transaction is rolled back. The row
    /// is saved before its version so that this check always comes first: the other request has already written the
    /// version this one would, and inserting it again would fail on the versions table's key instead.
    /// </remarks>
    /// <param name="current">The row as <see cref="FindForChangeAsync"/> read it.</param>
    /// <param name="changed">The row's new values.</param>
    /// <param name="version">The version that records the change.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns><see langword="true"/> when the change was saved; <see langword="false"/> when another save overtook
    /// it.</returns>
    public async Task<bool> SaveChangeAsync(
        Organization current,
        Organization changed,
        OrganizationVersion version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(changed);
        ArgumentNullException.ThrowIfNull(version);
        DatabaseFacade database = this._context.Database;
        IDbContextTransaction transaction = await database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            EntityEntry<Organization> entry = this._context.Entry(current);
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
    /// Reads organizations, filtered, narrowed by status and sorted, each with its number of contacts, starting just
    /// past a cursor's organization. A page before its cursor is read backwards from it, nearest first.
    /// </summary>
    /// <param name="request">The filter, status, sort and cursor to read.</param>
    /// <param name="cursorKey">How the list sorts the cursor's organization, or <see langword="null"/> to start at the
    /// beginning.</param>
    /// <param name="limit">How many organizations to read at most.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the organizations.</param>
    /// <returns>The organizations, in the order they were read.</returns>
    public Task<OrganizationSummary[]> ListAsync(
        OrganizationCollectionRequest request,
        OrganizationSortKey? cursorKey,
        RecordCount limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        IQueryable<Contact> contacts = this._context.Set<Contact>();
        IQueryable<Organization> matching = this.Match(request.Filter, request.Status);
        SortDirection direction = ReadDirection(request);
        if (cursorKey is not null)
        {
            matching = matching.Where(OrganizationSeek.Beyond(cursorKey, contacts, request.Sort, direction));
        }

        return Sort(matching, contacts, request.Sort, direction)
            .Take(limit.Value)
            .Select(organization => new OrganizationSummary
            {
                Id = organization.Id,
                Name = organization.Name,
                Industry = organization.Industry,
                Website = organization.Website,
                Contacts = new RecordCount(contacts.Count(contact => contact.OrganizationId == organization.Id)),
                LastModifiedOn = organization.LastModifiedOn,
                ArchivedOn = organization.ArchivedOn,
            })
            .ToArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Says which way to read: the list's own order, or its reverse for a page before the cursor.
    /// </summary>
    /// <param name="request">The request, with the list's order and the cursor's position.</param>
    /// <returns>The direction to read in.</returns>
    private static SortDirection ReadDirection(OrganizationCollectionRequest request)
    {
        if (request.Cursor?.Position != CursorPosition.Before)
        {
            return request.Direction;
        }

        return request.Direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
    }

    /// <summary>
    /// Sorts organizations by a column, then by identifier in the same direction, so that ties keep one order from page
    /// to page and an index on the column and the identifier can give the whole order.
    /// </summary>
    /// <param name="organizations">The organizations to sort.</param>
    /// <param name="contacts">Every contact, which the Contacts column counts.</param>
    /// <param name="column">What to sort by.</param>
    /// <param name="direction">Which way to sort.</param>
    /// <returns>The sorted organizations.</returns>
    private static IOrderedQueryable<Organization> Sort(
        IQueryable<Organization> organizations,
        IQueryable<Contact> contacts,
        OrganizationSortColumn column,
        SortDirection direction)
    {
        IOrderedQueryable<Organization> sorted = column switch
        {
            OrganizationSortColumn.Industry =>
                OrderBy(organizations, static organization => organization.Industry, direction),
            OrganizationSortColumn.Contacts =>
                OrderBy(
                    organizations,
                    organization => contacts.Count(contact => contact.OrganizationId == organization.Id),
                    direction),
            OrganizationSortColumn.Updated =>
                OrderBy(organizations, static organization => organization.LastModifiedOn, direction),
            _ =>
                OrderBy(organizations, static organization => organization.Name, direction),
        };
        return direction == SortDirection.Descending
            ? sorted.ThenByDescending(static organization => organization.Id)
            : sorted.ThenBy(static organization => organization.Id);
    }

    /// <summary>
    /// Orders organizations by one key in one direction.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="organizations">The organizations to order.</param>
    /// <param name="key">The key to order by.</param>
    /// <param name="direction">Which way to order.</param>
    /// <returns>The ordered organizations.</returns>
    private static IOrderedQueryable<Organization> OrderBy<TKey>(
        IQueryable<Organization> organizations,
        Expression<Func<Organization, TKey>> key,
        SortDirection direction)
    {
        return direction == SortDirection.Descending
            ? organizations.OrderByDescending(key)
            : organizations.OrderBy(key);
    }

    /// <summary>
    /// Makes a LIKE pattern that finds the text anywhere in a name, with any % or _ in it taken literally.
    /// </summary>
    /// <param name="filter">The text to find.</param>
    /// <returns>The pattern.</returns>
    private static string ContainsPattern(FilterText filter)
    {
        string escaped = filter.Value.Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal);
        escaped = escaped.Replace("%", LikeEscape + "%", StringComparison.Ordinal);
        escaped = escaped.Replace("_", LikeEscape + "_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }

    /// <summary>
    /// Narrows the organizations to those whose names contain the filter and whose status matches.
    /// </summary>
    /// <param name="filter">The text the names must contain, or <see langword="null"/> for every organization.</param>
    /// <param name="status">Which organizations to keep by whether they are archived.</param>
    /// <returns>The query for the matching organizations.</returns>
    private IQueryable<Organization> Match(FilterText? filter, RecordStatus status)
    {
        IQueryable<Organization> organizations = this._context.Set<Organization>();
        if (filter is FilterText text)
        {
            // SQLite's LIKE ignores the case of A to Z. The cast lets LIKE see the name as the text it is stored as.
            string pattern = ContainsPattern(text);
            organizations = organizations.Where(organization =>
                EF.Functions.Like((string)(object)organization.Name, pattern, LikeEscape));
        }

        return status switch
        {
            RecordStatus.Active =>
                organizations.Where(static organization => organization.ArchivedOn == null),
            RecordStatus.Archived =>
                organizations.Where(static organization => organization.ArchivedOn != null),
            _ =>
                organizations,
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
            // An expected outcome, not a failure: the caller tells the user, who decides whether to save again.
            return false;
        }
    }
}
