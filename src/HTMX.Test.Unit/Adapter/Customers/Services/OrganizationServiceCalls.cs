namespace HTMX.Test.Unit.Adapter.Customers.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Calls <see cref="IOrganizationService"/> as the pages do, over a seeded database of its own: one page, one
/// organization, or a page and then the pages its Previous and Next lead to.
/// </summary>
internal static class OrganizationServiceCalls
{
    // More pages than any list in the seed runs to, so a walk that never ends fails instead of hanging.
    private const int MaximumPages = 50;

    /// <summary>
    /// Makes the same request with a cursor, as a Previous or Next link would.
    /// </summary>
    /// <param name="request">The request to move.</param>
    /// <param name="cursor">Where the page lies, or <see langword="null"/> for the first page.</param>
    /// <returns>The request with the cursor.</returns>
    internal static OrganizationCollectionRequest At(OrganizationCollectionRequest request, OrganizationCursor? cursor)
    {
        return new OrganizationCollectionRequest
        {
            Filter = request.Filter,
            Status = request.Status,
            Sort = request.Sort,
            Direction = request.Direction,
            Cursor = cursor,
            PageSize = request.PageSize,
        };
    }

    /// <summary>
    /// Reads one page through the service.
    /// </summary>
    /// <param name="request">The page to read.</param>
    /// <returns>The page.</returns>
    internal static async Task<OrganizationCollectionResponse> ListAsync(OrganizationCollectionRequest request)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            return await ReadAsync(services, request).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads one organization through the service.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <returns>The organization, or <see langword="null"/> when there is none.</returns>
    internal static async Task<OrganizationSingleResponse?> FindAsync(OrganizationId id)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            using (IServiceScope scope = services.CreateScope())
            {
                IOrganizationService organizations = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
                return await organizations.FindAsync(id).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Reads a first page and then, in turn, the page each step leads to: Next for After, Previous for Before.
    /// </summary>
    /// <param name="first">The first page to read.</param>
    /// <param name="steps">The links to follow, one after another.</param>
    /// <returns>Every page read, the first page first.</returns>
    internal static async Task<OrganizationCollectionResponse[]> ReadInTurnAsync(
        OrganizationCollectionRequest first,
        params CursorPosition[] steps)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            return await FollowAsync(services, first, steps).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads a first page and then follows Next until there is no next page.
    /// </summary>
    /// <param name="first">The first page to read.</param>
    /// <returns>Every page read, in order.</returns>
    internal static async Task<OrganizationCollectionResponse[]> WalkToTheEndAsync(OrganizationCollectionRequest first)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            return await FollowNextAsync(services, first).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads a first page and then the page each step leads to, over one database.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="first">The first page to read.</param>
    /// <param name="steps">The links to follow, one after another.</param>
    /// <returns>Every page read, the first page first.</returns>
    private static async Task<OrganizationCollectionResponse[]> FollowAsync(
        ServiceProvider services,
        OrganizationCollectionRequest first,
        CursorPosition[] steps)
    {
        OrganizationCollectionResponse page = await ReadAsync(services, first).ConfigureAwait(false);
        List<OrganizationCollectionResponse> pages = [page];
        foreach (CursorPosition step in steps)
        {
            OrganizationCursor? cursor = step == CursorPosition.After ? page.Next : page.Previous;
            page = await ReadAsync(services, At(first, cursor)).ConfigureAwait(false);
            pages.Add(page);
        }

        return [.. pages];
    }

    /// <summary>
    /// Reads a first page and follows Next until there is no next page, over one database.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="first">The first page to read.</param>
    /// <returns>Every page read, in order.</returns>
    private static async Task<OrganizationCollectionResponse[]> FollowNextAsync(
        ServiceProvider services,
        OrganizationCollectionRequest first)
    {
        List<OrganizationCollectionResponse> pages = [];
        OrganizationCollectionRequest? request = first;
        while (request is not null && pages.Count < MaximumPages)
        {
            OrganizationCollectionResponse page = await ReadAsync(services, request).ConfigureAwait(false);
            pages.Add(page);
            request = page.Next is OrganizationCursor next ? At(first, next) : null;
        }

        return [.. pages];
    }

    /// <summary>
    /// Reads one page through the service, in a scope of its own as one request would.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="request">The page to read.</param>
    /// <returns>The page.</returns>
    private static async Task<OrganizationCollectionResponse> ReadAsync(
        ServiceProvider services,
        OrganizationCollectionRequest request)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            IOrganizationService organizations = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
            return await organizations.ListAsync(request).ConfigureAwait(false);
        }
    }
}
