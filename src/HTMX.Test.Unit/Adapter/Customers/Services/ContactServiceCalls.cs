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
/// Calls <see cref="IContactService"/> as the pages do, over a seeded database of its own: a page and then the pages
/// its Previous and Next lead to, one contact or their versions, or a new contact, an archive, a restore or a
/// roll-back.
/// </summary>
internal static class ContactServiceCalls
{
    /// <summary>
    /// Makes the request for the first page of an organization's contacts, ten to a page, as the Contacts tab does.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <param name="cursor">Where the page lies, or <see langword="null"/> for the first page.</param>
    /// <returns>The request.</returns>
    internal static ContactCollectionRequest PageOf(OrganizationId organization, ContactCursor? cursor = null)
    {
        return new ContactCollectionRequest
        {
            Organization = organization,
            Cursor = cursor,
            PageSize = new PageSize(10),
        };
    }

    /// <summary>
    /// Reads a first page and then, in turn, the page each step leads to: Next for After, Previous for Before.
    /// </summary>
    /// <param name="first">The first page to read.</param>
    /// <param name="steps">The links to follow, one after another.</param>
    /// <returns>Every page read, the first page first.</returns>
    internal static async Task<ContactCollectionResponse[]> ReadInTurnAsync(
        ContactCollectionRequest first,
        params CursorPosition[] steps)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            ContactCollectionResponse page = await ReadAsync(services, first).ConfigureAwait(false);
            List<ContactCollectionResponse> pages = [page];
            foreach (CursorPosition step in steps)
            {
                ContactCursor? cursor = step == CursorPosition.After ? page.Next : page.Previous;
                page = await ReadAsync(services, PageOf(first.Organization, cursor)).ConfigureAwait(false);
                pages.Add(page);
            }

            return [.. pages];
        }
    }

    /// <summary>
    /// Reads one contact through the service.
    /// </summary>
    /// <param name="id">The contact's identifier.</param>
    /// <returns>The contact, or <see langword="null"/> when there is none.</returns>
    internal static async Task<ContactSingleResponse?> FindAsync(ContactId id)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            using (IServiceScope scope = services.CreateScope())
            {
                IContactService contacts = scope.ServiceProvider.GetRequiredService<IContactService>();
                return await contacts.FindAsync(id).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Reads every version of one contact through the service, newest first, as their History tab does.
    /// </summary>
    /// <param name="id">The contact's identifier.</param>
    /// <returns>The versions, newest first.</returns>
    internal static async Task<ContactVersionResponse[]> ListVersionsAsync(ContactId id)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            using (IServiceScope scope = services.CreateScope())
            {
                IContactService contacts = scope.ServiceProvider.GetRequiredService<IContactService>();
                return await contacts.ListVersionsAsync(id).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Creates a contact through the service, then reads their row, and every version of them, straight from the
    /// database, and counts the contacts the database gained.
    /// </summary>
    /// <param name="request">The new contact.</param>
    /// <returns>What became of the request, the new contact and their versions, oldest first, or none and no versions
    /// when nothing was created, and how many contacts were added.</returns>
    internal static async Task<(ContactCreateResponse Response, Contact? After, ContactVersion[] History, int Added)>
        CreateAsync(ContactCreateRequest request)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            int before = await CountAsync(services).ConfigureAwait(false);
            ContactCreateResponse response;
            using (IServiceScope scope = services.CreateScope())
            {
                IContactService contacts = scope.ServiceProvider.GetRequiredService<IContactService>();
                response = await contacts.CreateAsync(request).ConfigureAwait(false);
            }

            int added = await CountAsync(services).ConfigureAwait(false) - before;
            if (response.Id is not ContactId id)
            {
                return (response, null, [], added);
            }

            (Contact? after, ContactVersion[] history) = await ReadBackAsync(services, id).ConfigureAwait(false);
            return (response, after, history, added);
        }
    }

    /// <summary>
    /// Sends a change to a contact through the service, then reads their row, and every version of them, straight from
    /// the database as they are afterwards.
    /// </summary>
    /// <param name="request">The change.</param>
    /// <returns>What became of the change, the contact afterwards, and their versions, oldest first.</returns>
    internal static async Task<(UpdateOutcome Outcome, Contact? After, ContactVersion[] History)> UpdateAsync(
        ContactUpdateRequest request)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            UpdateOutcome outcome;
            using (IServiceScope scope = services.CreateScope())
            {
                IContactService contacts = scope.ServiceProvider.GetRequiredService<IContactService>();
                outcome = await contacts.UpdateAsync(request).ConfigureAwait(false);
            }

            (Contact? after, ContactVersion[] history) =
                await ReadBackAsync(services, request.Id).ConfigureAwait(false);
            return (outcome, after, history);
        }
    }

    /// <summary>
    /// Rolls a contact back through the service, after archiving an organization first if asked, then reads their row,
    /// and every version of them, straight from the database as they are afterwards.
    /// </summary>
    /// <param name="request">The roll-back.</param>
    /// <param name="archivedFirst">An organization to archive before the roll-back, or <see langword="null"/> for
    /// none.</param>
    /// <returns>What became of the roll-back, the contact afterwards, and their versions, oldest first.</returns>
    internal static async Task<(UpdateOutcome Outcome, Contact? After, ContactVersion[] History)> RollBackAsync(
        ContactRollBackRequest request,
        OrganizationId? archivedFirst = null)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            if (archivedFirst is OrganizationId organization)
            {
                await ArchiveAsync(services, organization).ConfigureAwait(false);
            }

            UpdateOutcome outcome;
            using (IServiceScope scope = services.CreateScope())
            {
                IContactService contacts = scope.ServiceProvider.GetRequiredService<IContactService>();
                outcome = await contacts.RollBackAsync(request).ConfigureAwait(false);
            }

            (Contact? after, ContactVersion[] history) =
                await ReadBackAsync(services, request.Id).ConfigureAwait(false);
            return (outcome, after, history);
        }
    }

    /// <summary>
    /// Archives or restores a contact through the service, then reads their row, and every version of them, straight
    /// from the database as they are afterwards.
    /// </summary>
    /// <param name="request">Which contact, the organization the request was made among, and who asks.</param>
    /// <param name="change">Archived to archive them, Restored to restore them.</param>
    /// <returns>What became of the request, the contact afterwards, and their versions, oldest first.</returns>
    internal static async Task<(UpdateOutcome Outcome, Contact? After, ContactVersion[] History)> ChangeStatusAsync(
        ContactStatusRequest request,
        ChangeKind change)
    {
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            UpdateOutcome outcome;
            using (IServiceScope scope = services.CreateScope())
            {
                IContactService contacts = scope.ServiceProvider.GetRequiredService<IContactService>();
                outcome = change == ChangeKind.Archived
                    ? await contacts.ArchiveAsync(request).ConfigureAwait(false)
                    : await contacts.RestoreAsync(request).ConfigureAwait(false);
            }

            (Contact? after, ContactVersion[] history) =
                await ReadBackAsync(services, request.Id).ConfigureAwait(false);
            return (outcome, after, history);
        }
    }

    /// <summary>
    /// Archives an organization through its service, in a scope of its own as one request would.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="organization">The organization to archive.</param>
    /// <returns>A task that completes when it is archived.</returns>
    private static async Task ArchiveAsync(ServiceProvider services, OrganizationId organization)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            IOrganizationService organizations = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
            OrganizationStatusRequest request = new OrganizationStatusRequest
            {
                Id = organization,
                ChangedBy = UserSeed.DemoUser,
            };
            await organizations.ArchiveAsync(request).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Counts every contact, archived ones among them, straight from the database.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <returns>How many contacts the database holds.</returns>
    private static async Task<int> CountAsync(ServiceProvider services)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            CustomersDatabaseContext context = scope.ServiceProvider.GetRequiredService<CustomersDatabaseContext>();
            return await context.Set<Contact>().CountAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads a contact's row, and every version of them, oldest first, straight from the database.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="id">The contact's identifier.</param>
    /// <returns>The row, or <see langword="null"/> when there is none, and the versions.</returns>
    private static async Task<(Contact? After, ContactVersion[] History)> ReadBackAsync(
        ServiceProvider services,
        ContactId id)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            CustomersDatabaseContext context = scope.ServiceProvider.GetRequiredService<CustomersDatabaseContext>();
            Contact? after = await context.Set<Contact>()
                .FirstOrDefaultAsync(contact => contact.Id == id)
                .ConfigureAwait(false);
            ContactVersion[] versions = await context.Set<ContactVersion>()
                .Where(version => version.ContactId == id)
                .ToArrayAsync()
                .ConfigureAwait(false);
            return (after, [.. versions.OrderBy(static version => version.Version.Value)]);
        }
    }

    /// <summary>
    /// Reads one page through the service, in a scope of its own as one request would.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="request">The page to read.</param>
    /// <returns>The page.</returns>
    private static async Task<ContactCollectionResponse> ReadAsync(
        ServiceProvider services,
        ContactCollectionRequest request)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            IContactService contacts = scope.ServiceProvider.GetRequiredService<IContactService>();
            return await contacts.ListAsync(request).ConfigureAwait(false);
        }
    }
}
