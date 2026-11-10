namespace HTMX.Test.Unit.Adapter.Customers.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Calls <see cref="IUserService"/> as the users list does, over a seeded database of its own: one page, a page and
/// then the pages its Previous and Next lead to, or every page from the first to the last.
/// </summary>
internal static class UserServiceCalls
{
    // More pages than the users list runs to at any size, so a walk that never ends fails instead of hanging.
    private const int MaximumPages = 50;

    /// <summary>
    /// Makes the same request with a cursor, as a Previous or Next link would.
    /// </summary>
    /// <param name="request">The request to move.</param>
    /// <param name="cursor">Where the page lies, or <see langword="null"/> for the first page.</param>
    /// <returns>The request with the cursor.</returns>
    internal static UserCollectionRequest At(UserCollectionRequest request, UserCursor? cursor)
    {
        return new UserCollectionRequest
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
    internal static async Task<UserCollectionResponse> ListAsync(UserCollectionRequest request)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            return await ReadAsync(services, request).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads a first page and then, in turn, the page each step leads to: Next for After, Previous for Before.
    /// </summary>
    /// <param name="first">The first page to read.</param>
    /// <param name="steps">The links to follow, one after another.</param>
    /// <returns>Every page read, the first page first.</returns>
    internal static async Task<UserCollectionResponse[]> ReadInTurnAsync(
        UserCollectionRequest first,
        params CursorPosition[] steps)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            UserCollectionResponse page = await ReadAsync(services, first).ConfigureAwait(false);
            List<UserCollectionResponse> pages = [page];
            foreach (CursorPosition step in steps)
            {
                UserCursor? cursor = step == CursorPosition.After ? page.Next : page.Previous;
                page = await ReadAsync(services, At(first, cursor)).ConfigureAwait(false);
                pages.Add(page);
            }

            return [.. pages];
        }
    }

    /// <summary>
    /// Reads a first page and then follows Next until there is no next page.
    /// </summary>
    /// <param name="first">The first page to read.</param>
    /// <returns>Every page read, in order.</returns>
    internal static async Task<UserCollectionResponse[]> WalkToTheEndAsync(UserCollectionRequest first)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            List<UserCollectionResponse> pages = [];
            UserCollectionRequest? request = first;
            while (request is not null && pages.Count < MaximumPages)
            {
                UserCollectionResponse page = await ReadAsync(services, request).ConfigureAwait(false);
                pages.Add(page);
                request = page.Next is UserCursor next ? At(first, next) : null;
            }

            return [.. pages];
        }
    }

    /// <summary>
    /// Reads one page through the service, in a scope of its own as one request would.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="request">The page to read.</param>
    /// <returns>The page.</returns>
    private static async Task<UserCollectionResponse> ReadAsync(ServiceProvider services, UserCollectionRequest request)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            IUserService users = scope.ServiceProvider.GetRequiredService<IUserService>();
            return await users.ListAsync(request).ConfigureAwait(false);
        }
    }
}
