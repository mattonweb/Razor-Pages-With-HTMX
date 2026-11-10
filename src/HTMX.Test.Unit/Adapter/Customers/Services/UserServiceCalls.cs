namespace HTMX.Test.Unit.Adapter.Customers.Services;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Calls <see cref="IUserService"/> as the users' pages do, over a seeded database of its own: one page, a page and
/// then the pages its Previous and Next lead to, every page from the first to the last, one user or their versions, or
/// an archive, a restore or a roll-back.
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
    /// Reads one user through the service.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <returns>The user, or <see langword="null"/> when there is none.</returns>
    internal static async Task<UserSingleResponse?> FindAsync(UserId id)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            return await FindAsync(services, id).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads every version of one user through the service, newest first, as their History tab does.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <returns>The versions, newest first.</returns>
    internal static async Task<UserVersionResponse[]> ListVersionsAsync(UserId id)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            using (IServiceScope scope = services.CreateScope())
            {
                IUserService users = scope.ServiceProvider.GetRequiredService<IUserService>();
                return await users.ListVersionsAsync(id).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Archives or restores a user through the service, then reads them, and every version of them, as they are
    /// afterwards.
    /// </summary>
    /// <param name="request">Which user, and who asks.</param>
    /// <param name="change">Archived to archive them, Restored to restore them.</param>
    /// <returns>What became of the request, the user afterwards, and their versions, oldest first.</returns>
    internal static async Task<(UpdateOutcome Outcome, UserSingleResponse? After, UserVersion[] History)>
        ChangeStatusAsync(UserStatusRequest request, ChangeKind change)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            UpdateOutcome outcome;
            using (IServiceScope scope = services.CreateScope())
            {
                IUserService users = scope.ServiceProvider.GetRequiredService<IUserService>();
                outcome = change == ChangeKind.Archived
                    ? await users.ArchiveAsync(request).ConfigureAwait(false)
                    : await users.RestoreAsync(request).ConfigureAwait(false);
            }

            UserSingleResponse? after = await FindAsync(services, request.Id).ConfigureAwait(false);
            UserVersion[] history = await ReadHistoryAsync(services, request.Id).ConfigureAwait(false);
            return (outcome, after, history);
        }
    }

    /// <summary>
    /// Rolls a user back through the service, after giving other users new values first, then reads the user, and
    /// every version of them, as they are afterwards.
    /// </summary>
    /// <remarks>
    /// Users have no edit form yet, so a value another user has taken since an earlier version is written straight to
    /// their row, without a version, as an edit would leave it.
    /// </remarks>
    /// <param name="request">The roll-back.</param>
    /// <param name="givenFirst">Users to give a new username or email address before the roll-back, if any.</param>
    /// <returns>What became of the roll-back, the user afterwards, and their versions, oldest first.</returns>
    internal static async Task<(UpdateOutcome Outcome, UserSingleResponse? After, UserVersion[] History)>
        RollBackAsync(UserRollBackRequest request, params (UserId Id, string? Username, string? Email)[] givenFirst)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            foreach ((UserId Id, string? Username, string? Email) values in givenFirst)
            {
                await GiveAsync(services, values).ConfigureAwait(false);
            }

            UpdateOutcome outcome;
            using (IServiceScope scope = services.CreateScope())
            {
                IUserService users = scope.ServiceProvider.GetRequiredService<IUserService>();
                outcome = await users.RollBackAsync(request).ConfigureAwait(false);
            }

            UserSingleResponse? after = await FindAsync(services, request.Id).ConfigureAwait(false);
            UserVersion[] history = await ReadHistoryAsync(services, request.Id).ConfigureAwait(false);
            return (outcome, after, history);
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
    /// Reads one user through the service, in a scope of its own as one request would.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="id">The user's identifier.</param>
    /// <returns>The user, or <see langword="null"/> when there is none.</returns>
    private static async Task<UserSingleResponse?> FindAsync(ServiceProvider services, UserId id)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            IUserService users = scope.ServiceProvider.GetRequiredService<IUserService>();
            return await users.FindAsync(id).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads every version of one user, oldest first, straight from the database.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="id">The user's identifier.</param>
    /// <returns>The versions.</returns>
    private static async Task<UserVersion[]> ReadHistoryAsync(ServiceProvider services, UserId id)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            CustomersDatabaseContext context = scope.ServiceProvider.GetRequiredService<CustomersDatabaseContext>();
            UserVersion[] versions = await context.Set<UserVersion>()
                .Where(version => version.UserId == id)
                .ToArrayAsync()
                .ConfigureAwait(false);
            return [.. versions.OrderBy(static version => version.Version.Value)];
        }
    }

    /// <summary>
    /// Gives a user a new username or email address straight in their row, without a version.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="values">The user, and the username or email address to give them; either may be left as it
    /// is.</param>
    /// <returns>A task that completes when the row is saved.</returns>
    private static async Task GiveAsync(ServiceProvider services, (UserId Id, string? Username, string? Email) values)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            CustomersDatabaseContext context = scope.ServiceProvider.GetRequiredService<CustomersDatabaseContext>();
            User current = await context.Set<User>().FirstAsync(row => row.Id == values.Id).ConfigureAwait(false);
            User changed = current with
            {
                Username = values.Username is null ? current.Username : new Username(values.Username),
                Email = values.Email is null ? current.Email : new EmailAddress(values.Email),
            };
            context.Entry(current).CurrentValues.SetValues(changed);
            await context.SaveChangesAsync().ConfigureAwait(false);
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
