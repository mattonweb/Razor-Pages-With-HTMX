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
            matching = matching.Where(Beyond(cursorKey, contacts, request.Sort, direction));
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
    /// Keeps the organizations that come after the cursor's in the order the page is read: the seek of key-based
    /// paging.
    /// </summary>
    /// <param name="key">How the list sorts the cursor's organization.</param>
    /// <param name="contacts">Every contact, which the Contacts column counts.</param>
    /// <param name="column">What the list is sorted by.</param>
    /// <param name="direction">Which way the page is read.</param>
    /// <returns>The condition.</returns>
    private static Expression<Func<Organization, bool>> Beyond(
        OrganizationSortKey key,
        IQueryable<Contact> contacts,
        OrganizationSortColumn column,
        SortDirection direction)
    {
        return direction == SortDirection.Descending
            ? Earlier(key, contacts, column)
            : Later(key, contacts, column);
    }

    /// <summary>
    /// Keeps the organizations that come after the key when the list is read smallest first: a greater value in the
    /// sorted column, or the same value and a greater identifier.
    /// </summary>
    /// <remarks>
    /// Each condition says "at least the key's value, and then past the key", rather than "a greater value, or the same
    /// value and a greater identifier". The first half alone is a range that the index on the column and the
    /// identifier can seek to; the second half only settles the ties at its edge. LINQ cannot compare two columns as a
    /// pair, so the pair is written out. The comparisons are on the wrapped values themselves, which Entity Framework
    /// turns into SQL on the bare columns; a cast to the stored type would become a SQL CAST, and the database would
    /// read the index from its start instead of seeking. Contacts is a count worked out for every organization, which
    /// no index can hold: its condition is right, but the database reads every organization to answer it.
    /// </remarks>
    /// <param name="key">How the list sorts the cursor's organization.</param>
    /// <param name="contacts">Every contact, which the Contacts column counts.</param>
    /// <param name="column">What the list is sorted by.</param>
    /// <returns>The condition.</returns>
    private static Expression<Func<Organization, bool>> Later(
        OrganizationSortKey key,
        IQueryable<Contact> contacts,
        OrganizationSortColumn column)
    {
        OrganizationId id = key.Id;
        OrganizationName name = key.Name;
        Industry industry = key.Industry;
        int contactCount = key.Contacts.Value;
        DateTime changed = key.LastModifiedOn;
        return column switch
        {
            OrganizationSortColumn.Industry =>
                organization => organization.Industry >= industry
                    && (organization.Industry > industry || organization.Id > id),
            OrganizationSortColumn.Contacts =>
                organization => contacts.Count(contact => contact.OrganizationId == organization.Id) >= contactCount
                    && (contacts.Count(contact => contact.OrganizationId == organization.Id) > contactCount
                        || organization.Id > id),
            OrganizationSortColumn.Updated =>
                organization => organization.LastModifiedOn >= changed
                    && (organization.LastModifiedOn > changed || organization.Id > id),
            _ =>
                organization => organization.Name >= name
                    && (organization.Name > name || organization.Id > id),
        };
    }

    /// <summary>
    /// Keeps the organizations that come after the key when the list is read largest first: a smaller value in the
    /// sorted column, or the same value and a smaller identifier. The conditions are <see cref="Later"/>'s, turned
    /// round.
    /// </summary>
    /// <param name="key">How the list sorts the cursor's organization.</param>
    /// <param name="contacts">Every contact, which the Contacts column counts.</param>
    /// <param name="column">What the list is sorted by.</param>
    /// <returns>The condition.</returns>
    private static Expression<Func<Organization, bool>> Earlier(
        OrganizationSortKey key,
        IQueryable<Contact> contacts,
        OrganizationSortColumn column)
    {
        OrganizationId id = key.Id;
        OrganizationName name = key.Name;
        Industry industry = key.Industry;
        int contactCount = key.Contacts.Value;
        DateTime changed = key.LastModifiedOn;
        return column switch
        {
            OrganizationSortColumn.Industry =>
                organization => organization.Industry <= industry
                    && (organization.Industry < industry || organization.Id < id),
            OrganizationSortColumn.Contacts =>
                organization => contacts.Count(contact => contact.OrganizationId == organization.Id) <= contactCount
                    && (contacts.Count(contact => contact.OrganizationId == organization.Id) < contactCount
                        || organization.Id < id),
            OrganizationSortColumn.Updated =>
                organization => organization.LastModifiedOn <= changed
                    && (organization.LastModifiedOn < changed || organization.Id < id),
            _ =>
                organization => organization.Name <= name
                    && (organization.Name < name || organization.Id < id),
        };
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
}
