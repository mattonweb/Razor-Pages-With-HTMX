namespace HTMX.Adapter.Customers.Services;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.DTOs;
using HTMX.Adapter.Customers.Providers;

/// <summary>
/// The rules over organizations, behind <see cref="IOrganizationService"/>; the queries themselves are the provider's.
/// </summary>
internal sealed class OrganizationService : IOrganizationService
{
    private readonly OrganizationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationService"/> class.
    /// </summary>
    /// <param name="provider">Runs the queries.</param>
    public OrganizationService(OrganizationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        this._provider = provider;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A page is read one organization longer than it shows: if that extra organization is there, so is another page,
    /// and no count is needed to know it. A cursor whose organization no longer exists, or that leaves too little to
    /// show, starts again at the first page, so an old link never shows an empty or a short page.
    /// </remarks>
    public async Task<OrganizationCollectionResponse> ListAsync(
        OrganizationCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RecordCount active = await this._provider
            .CountAsync(request.Filter, RecordStatus.Active, cancellationToken)
            .ConfigureAwait(false);
        RecordCount archived = await this._provider
            .CountAsync(request.Filter, RecordStatus.Archived, cancellationToken)
            .ConfigureAwait(false);

        OrganizationSortKey? key = await this.FindCursorKeyAsync(request.Cursor, cancellationToken).ConfigureAwait(false);
        OrganizationCollectionRequest answered = key is null ? FirstPage(request) : request;
        OrganizationSummary[] read = await this.ReadAsync(answered, key, cancellationToken).ConfigureAwait(false);
        if (key is not null && IsTooShort(answered, read))
        {
            answered = FirstPage(request);
            read = await this.ReadAsync(answered, null, cancellationToken).ConfigureAwait(false);
        }

        return CreatePage(answered, read, active, archived);
    }

    /// <summary>
    /// Says whether a cursor left too little to show: nothing at all past it, or a short page before it, which means
    /// the page reached the start of the list, and the first page, read forwards, should be shown whole instead.
    /// </summary>
    /// <param name="request">The request that was read.</param>
    /// <param name="read">The organizations read for it.</param>
    /// <returns><see langword="true"/> when the first page should be read instead.</returns>
    private static bool IsTooShort(OrganizationCollectionRequest request, OrganizationSummary[] read)
    {
        bool backwards = request.Cursor?.Position == CursorPosition.Before;
        return read.Length == 0 || (backwards && read.Length < request.PageSize.Value);
    }

    /// <summary>
    /// Makes the request for the first page of the same list: the same filter, status, sort and size, and no cursor.
    /// </summary>
    /// <param name="request">The request to start again from.</param>
    /// <returns>The request for the first page.</returns>
    private static OrganizationCollectionRequest FirstPage(OrganizationCollectionRequest request)
    {
        // Written out rather than with a with expression, which StyleCop 1.1.118 misreads in an instance class (SA1101).
        return new OrganizationCollectionRequest
        {
            Filter = request.Filter,
            Status = request.Status,
            Sort = request.Sort,
            Direction = request.Direction,
            PageSize = request.PageSize,
        };
    }

    /// <summary>
    /// Turns the organizations read into a page in the list's order, with the counts and the cursors for the pages
    /// either side. The extra organization read, if any, is dropped; it only showed that another page exists.
    /// </summary>
    /// <param name="request">The request the page answers.</param>
    /// <param name="read">The organizations read for it, in the order they were read.</param>
    /// <param name="active">How many active organizations match the filter.</param>
    /// <param name="archived">How many archived organizations match the filter.</param>
    /// <returns>The page.</returns>
    private static OrganizationCollectionResponse CreatePage(
        OrganizationCollectionRequest request,
        OrganizationSummary[] read,
        RecordCount active,
        RecordCount archived)
    {
        bool backwards = request.Cursor?.Position == CursorPosition.Before;
        bool more = read.Length > request.PageSize.Value;
        OrganizationSummary[] shown = [.. read.Take(request.PageSize.Value)];
        OrganizationSummary[] items = backwards ? [.. Enumerable.Reverse(shown)] : shown;

        // Read forwards, the extra organization means a next page, and any cursor means a previous one. Read
        // backwards, it is the other way round.
        bool hasPrevious = backwards ? more : request.Cursor is not null;
        bool hasNext = backwards || more;
        RecordCount all = new RecordCount(active.Value + archived.Value);
        return new OrganizationCollectionResponse
        {
            Request = request,
            Items = items,
            Previous = hasPrevious ? Edge(items, 0, CursorPosition.Before) : null,
            Next = hasNext ? Edge(items, items.Length - 1, CursorPosition.After) : null,
            Matching = Matching(request.Status, active, archived, all),
            Active = active,
            Archived = archived,
            All = all,
        };
    }

    /// <summary>
    /// Picks the count that matches the list's status.
    /// </summary>
    /// <param name="status">Which organizations the list shows.</param>
    /// <param name="active">How many active organizations match the filter.</param>
    /// <param name="archived">How many archived organizations match the filter.</param>
    /// <param name="all">How many organizations match the filter.</param>
    /// <returns>How many organizations the list holds, on every page together.</returns>
    private static RecordCount Matching(RecordStatus status, RecordCount active, RecordCount archived, RecordCount all)
    {
        return status switch
        {
            RecordStatus.Active =>
                active,
            RecordStatus.Archived =>
                archived,
            _ =>
                all,
        };
    }

    /// <summary>
    /// Makes the cursor for the page beside one of a page's organizations.
    /// </summary>
    /// <param name="items">The page's organizations.</param>
    /// <param name="index">Which organization: the first, for the page before, or the last, for the page after.</param>
    /// <param name="position">Which side of that organization the other page lies on.</param>
    /// <returns>The cursor, or <see langword="null"/> when the page is empty.</returns>
    private static OrganizationCursor? Edge(OrganizationSummary[] items, int index, CursorPosition position)
    {
        if (items.Length == 0)
        {
            return null;
        }

        return new OrganizationCursor(items[index].Id, position);
    }

    /// <summary>
    /// Reads how the list sorts the organization a cursor names.
    /// </summary>
    /// <param name="cursor">The cursor, or <see langword="null"/> for the first page.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the values.</param>
    /// <returns>The values, or <see langword="null"/> for no cursor or an organization that no longer exists.</returns>
    private async Task<OrganizationSortKey?> FindCursorKeyAsync(
        OrganizationCursor? cursor,
        CancellationToken cancellationToken)
    {
        if (cursor is not OrganizationCursor named)
        {
            return null;
        }

        return await this._provider.FindSortKeyAsync(named.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads one organization more than a page holds, starting just past the cursor's organization.
    /// </summary>
    /// <param name="request">The page to read.</param>
    /// <param name="key">How the list sorts the cursor's organization, or <see langword="null"/> for the first
    /// page.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the organizations.</param>
    /// <returns>The organizations, in the order they were read.</returns>
    private Task<OrganizationSummary[]> ReadAsync(
        OrganizationCollectionRequest request,
        OrganizationSortKey? key,
        CancellationToken cancellationToken)
    {
        RecordCount limit = new RecordCount(request.PageSize.Value + 1);
        return this._provider.ListAsync(request, key, limit, cancellationToken);
    }
}
