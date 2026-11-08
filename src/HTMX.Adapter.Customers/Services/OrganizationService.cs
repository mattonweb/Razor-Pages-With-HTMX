namespace HTMX.Adapter.Customers.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
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
        RecordCount all = new RecordCount(active.Value + archived.Value);
        RecordCount matching = request.Status switch
        {
            RecordStatus.Active =>
                active,
            RecordStatus.Archived =>
                archived,
            _ =>
                all,
        };

        // Written out rather than with a with expression, which StyleCop 1.1.118 misreads in an instance class (SA1101).
        OrganizationCollectionRequest answered = new OrganizationCollectionRequest
        {
            Filter = request.Filter,
            Status = request.Status,
            Sort = request.Sort,
            Direction = request.Direction,
            Page = LastPageAtMost(request, matching),
            PageSize = request.PageSize,
        };
        OrganizationSummary[] items = await this._provider.ListAsync(answered, cancellationToken).ConfigureAwait(false);
        return new OrganizationCollectionResponse
        {
            Request = answered,
            Items = items,
            Matching = matching,
            Active = active,
            Archived = archived,
            All = all,
        };
    }

    /// <summary>
    /// Moves a page number past the end of the list back to its last page, so an old link never shows an empty page.
    /// </summary>
    /// <param name="request">The request, with the page it asked for and the size of a page.</param>
    /// <param name="matching">How many organizations the list holds.</param>
    /// <returns>The page asked for, or the last page if that comes first.</returns>
    private static PageNumber LastPageAtMost(OrganizationCollectionRequest request, RecordCount matching)
    {
        int lastPage = Math.Max(1, (matching.Value + request.PageSize.Value - 1) / request.PageSize.Value);
        return new PageNumber(Math.Min(request.Page.Value, lastPage));
    }
}
