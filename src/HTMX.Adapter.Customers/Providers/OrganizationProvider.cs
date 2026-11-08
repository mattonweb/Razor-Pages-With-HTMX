namespace HTMX.Adapter.Customers.Providers;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data;
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
    /// Reads one page of organizations, filtered, narrowed by status and sorted, each with its number of contacts.
    /// </summary>
    /// <param name="request">The filter, status, sort and page to read.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the page.</param>
    /// <returns>The organizations on the page, in order.</returns>
    public Task<OrganizationSummary[]> ListAsync(
        OrganizationCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        IQueryable<Contact> contacts = this._context.Set<Contact>();
        IQueryable<Organization> matching = this.Match(request.Filter, request.Status);
        IOrderedQueryable<Organization> sorted = Sort(matching, contacts, request.Sort, request.Direction);
        int skipped = (request.Page.Value - 1) * request.PageSize.Value;
        return sorted
            .Skip(skipped)
            .Take(request.PageSize.Value)
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
    /// Sorts organizations by a column, then by identifier so that ties keep one order from page to page.
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
        return sorted.ThenBy(static organization => organization.Id);
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
