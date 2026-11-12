namespace HTMX.Test.Unit.Adapter.Customers.Services;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data;
using HTMX.Adapter.Customers.Data.Seed;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Calls <see cref="IOrganizationService"/> as the pages do, over a seeded database of its own: one page, one
/// organization, a page and then the pages its Previous and Next lead to, or changes sent one after another.
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
            return await FindAsync(services, id).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asks the service whether an organization other than the one given has a name.
    /// </summary>
    /// <param name="name">The name to ask about.</param>
    /// <param name="except">The organization whose own name does not count, or <see langword="null"/> for none.</param>
    /// <returns><see langword="true"/> when another organization has the name.</returns>
    internal static async Task<bool> IsNameTakenAsync(OrganizationName name, OrganizationId? except)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            using (IServiceScope scope = services.CreateScope())
            {
                IOrganizationService organizations = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
                return await organizations.IsNameTakenAsync(name, except).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Sends changes to one organization one after another, each in a request of its own as two browser tabs would,
    /// then reads the organization and every version of it.
    /// </summary>
    /// <param name="id">The organization the changes are for.</param>
    /// <param name="requests">The changes, in the order they are sent.</param>
    /// <returns>What became of each change, the organization afterwards, and its versions, oldest first.</returns>
    internal static async Task<(UpdateOutcome[] Outcomes, OrganizationSingleResponse? After, OrganizationVersion[] History)>
        UpdateInTurnAsync(OrganizationId id, params OrganizationUpdateRequest[] requests)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            UpdateOutcome[] outcomes = await UpdateEachAsync(services, requests).ConfigureAwait(false);
            OrganizationSingleResponse? after = await FindAsync(services, id).ConfigureAwait(false);
            OrganizationVersion[] history = await ReadHistoryAsync(services, id).ConfigureAwait(false);
            return (outcomes, after, history);
        }
    }

    /// <summary>
    /// Creates an organization through the service, then reads the organization at an identifier, and every version of
    /// it, as they are afterwards.
    /// </summary>
    /// <param name="request">The new organization.</param>
    /// <param name="readBack">The identifier to read afterwards: the one the organization should be given.</param>
    /// <returns>What became of the request, the organization at the identifier, and its versions, oldest
    /// first.</returns>
    internal static async Task<(OrganizationCreateResponse Response, OrganizationSingleResponse? After, OrganizationVersion[] History)>
        CreateAsync(OrganizationCreateRequest request, OrganizationId readBack)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            OrganizationCreateResponse response;
            using (IServiceScope scope = services.CreateScope())
            {
                IOrganizationService organizations = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
                response = await organizations.CreateAsync(request).ConfigureAwait(false);
            }

            OrganizationSingleResponse? after = await FindAsync(services, readBack).ConfigureAwait(false);
            OrganizationVersion[] history = await ReadHistoryAsync(services, readBack).ConfigureAwait(false);
            return (response, after, history);
        }
    }

    /// <summary>
    /// Archives or restores an organization through the service, then reads it, and every version of it, as they are
    /// afterwards.
    /// </summary>
    /// <param name="id">The organization to archive or restore.</param>
    /// <param name="change">Archived to archive it, Restored to restore it.</param>
    /// <returns>What became of the request, the organization afterwards, and its versions, oldest first.</returns>
    internal static async Task<(UpdateOutcome Outcome, OrganizationSingleResponse? After, OrganizationVersion[] History)>
        ChangeStatusAsync(OrganizationId id, ChangeKind change)
    {
        OrganizationStatusRequest request = new OrganizationStatusRequest { Id = id, ChangedBy = UserSeed.DemoUser };
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            UpdateOutcome outcome;
            using (IServiceScope scope = services.CreateScope())
            {
                IOrganizationService organizations = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
                outcome = change == ChangeKind.Archived
                    ? await organizations.ArchiveAsync(request).ConfigureAwait(false)
                    : await organizations.RestoreAsync(request).ConfigureAwait(false);
            }

            OrganizationSingleResponse? after = await FindAsync(services, id).ConfigureAwait(false);
            OrganizationVersion[] history = await ReadHistoryAsync(services, id).ConfigureAwait(false);
            return (outcome, after, history);
        }
    }

    /// <summary>
    /// Deletes an organization through the service, after archiving it first when asked, then reads it, every version
    /// of it, and the versions of one contact, as they are afterwards.
    /// </summary>
    /// <param name="request">The delete.</param>
    /// <param name="archivedFirst">Whether to archive the organization, as Demo User, before the delete.</param>
    /// <param name="contact">A contact whose versions to read afterwards.</param>
    /// <returns>What became of the delete, the organization afterwards, its versions, oldest first, and the contact's
    /// versions, newest first.</returns>
    internal static async Task<(DeleteOutcome Outcome, OrganizationSingleResponse? After, OrganizationVersion[] History, ContactVersionResponse[] Contact)>
        DeleteAsync(OrganizationDeleteRequest request, bool archivedFirst, ContactId contact)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            DeleteOutcome outcome;
            using (IServiceScope scope = services.CreateScope())
            {
                IOrganizationService organizations = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
                if (archivedFirst)
                {
                    OrganizationStatusRequest archive = new OrganizationStatusRequest
                    {
                        Id = request.Id,
                        ChangedBy = UserSeed.DemoUser,
                    };
                    await organizations.ArchiveAsync(archive).ConfigureAwait(false);
                }

                outcome = await organizations.DeleteAsync(request).ConfigureAwait(false);
            }

            OrganizationSingleResponse? after = await FindAsync(services, request.Id).ConfigureAwait(false);
            OrganizationVersion[] history = await ReadHistoryAsync(services, request.Id).ConfigureAwait(false);
            using (IServiceScope scope = services.CreateScope())
            {
                IContactService contacts = scope.ServiceProvider.GetRequiredService<IContactService>();
                ContactVersionResponse[] versions = await contacts.ListVersionsAsync(contact).ConfigureAwait(false);
                return (outcome, after, history, versions);
            }
        }
    }

    /// <summary>
    /// Reads every version of one organization through the service, newest first, as its History tab does.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <returns>The versions, newest first.</returns>
    internal static async Task<OrganizationVersionResponse[]> ListVersionsAsync(OrganizationId id)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            using (IServiceScope scope = services.CreateScope())
            {
                IOrganizationService organizations = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
                return await organizations.ListVersionsAsync(id).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Rolls an organization back through the service, then reads it, and every version of it, as they are afterwards.
    /// </summary>
    /// <param name="request">The roll-back.</param>
    /// <returns>What became of it, the organization afterwards, and its versions, oldest first.</returns>
    internal static async Task<(UpdateOutcome Outcome, OrganizationSingleResponse? After, OrganizationVersion[] History)>
        RollBackAsync(OrganizationRollBackRequest request)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            UpdateOutcome outcome;
            using (IServiceScope scope = services.CreateScope())
            {
                IOrganizationService organizations = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
                outcome = await organizations.RollBackAsync(request).ConfigureAwait(false);
            }

            OrganizationSingleResponse? after = await FindAsync(services, request.Id).ConfigureAwait(false);
            OrganizationVersion[] history = await ReadHistoryAsync(services, request.Id).ConfigureAwait(false);
            return (outcome, after, history);
        }
    }

    /// <summary>
    /// Reads every version of one organization, oldest first, straight from the database.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="id">The organization's identifier.</param>
    /// <returns>The versions.</returns>
    internal static async Task<OrganizationVersion[]> ReadHistoryAsync(ServiceProvider services, OrganizationId id)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            CustomersDatabaseContext context = scope.ServiceProvider.GetRequiredService<CustomersDatabaseContext>();
            IQueryable<OrganizationVersion> all = context.Set<OrganizationVersion>();
            OrganizationVersion[] versions = await all
                .Where(version => version.OrganizationId == id)
                .ToArrayAsync()
                .ConfigureAwait(false);
            return [.. versions.OrderBy(static version => version.Version.Value)];
        }
    }

    /// <summary>
    /// Sends one change through the service, in a scope of its own as one request would.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="request">The change.</param>
    /// <returns>What became of it.</returns>
    internal static async Task<UpdateOutcome> UpdateAsync(ServiceProvider services, OrganizationUpdateRequest request)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            IOrganizationService organizations = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
            return await organizations.UpdateAsync(request).ConfigureAwait(false);
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
    /// Sends changes through the service one after another, each in a scope of its own as one request would.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="requests">The changes, in the order they are sent.</param>
    /// <returns>What became of each change.</returns>
    private static async Task<UpdateOutcome[]> UpdateEachAsync(
        ServiceProvider services,
        OrganizationUpdateRequest[] requests)
    {
        List<UpdateOutcome> outcomes = [];
        foreach (OrganizationUpdateRequest request in requests)
        {
            UpdateOutcome outcome = await UpdateAsync(services, request).ConfigureAwait(false);
            outcomes.Add(outcome);
        }

        return [.. outcomes];
    }

    /// <summary>
    /// Reads one organization through the service, in a scope of its own as one request would.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="id">The organization's identifier.</param>
    /// <returns>The organization, or <see langword="null"/> when there is none.</returns>
    private static async Task<OrganizationSingleResponse?> FindAsync(ServiceProvider services, OrganizationId id)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            IOrganizationService organizations = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
            return await organizations.FindAsync(id).ConfigureAwait(false);
        }
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
