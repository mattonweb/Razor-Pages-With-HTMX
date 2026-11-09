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
/// Calls <see cref="IContactService"/> as the Contacts tab does, over a seeded database of its own: a page and then the
/// pages its Previous and Next lead to, or an archive or a restore.
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

            using (IServiceScope scope = services.CreateScope())
            {
                CustomersDatabaseContext context = scope.ServiceProvider.GetRequiredService<CustomersDatabaseContext>();
                ContactId id = request.Id;
                Contact? after = await context.Set<Contact>()
                    .FirstOrDefaultAsync(contact => contact.Id == id)
                    .ConfigureAwait(false);
                ContactVersion[] versions = await context.Set<ContactVersion>()
                    .Where(version => version.ContactId == id)
                    .ToArrayAsync()
                    .ConfigureAwait(false);
                return (outcome, after, [.. versions.OrderBy(static version => version.Version.Value)]);
            }
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
