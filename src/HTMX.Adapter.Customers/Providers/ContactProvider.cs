namespace HTMX.Adapter.Customers.Providers;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data;
using HTMX.Adapter.Customers.Data.Conversions;
using HTMX.Adapter.Customers.DTOs;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// The Entity Framework queries over contacts: each method is one query, and what it sends is the SQL the console
/// shows.
/// </summary>
internal sealed class ContactProvider
{
    private readonly CustomersDatabaseContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactProvider"/> class.
    /// </summary>
    /// <param name="context">The context of the request the queries run for.</param>
    public ContactProvider(CustomersDatabaseContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this._context = context;
    }

    /// <summary>
    /// Counts an organization's contacts, archived ones included.
    /// </summary>
    /// <param name="organization">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the count.</param>
    /// <returns>The number of contacts.</returns>
    public async Task<RecordCount> CountAsync(
        OrganizationId organization,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Contact> contacts = this._context.Set<Contact>();
        int count = await contacts
            .CountAsync(contact => contact.OrganizationId == organization, cancellationToken)
            .ConfigureAwait(false);
        return new RecordCount(count);
    }

    /// <summary>
    /// Reads how the list sorts the contact a cursor names, as long as they still belong to the organization.
    /// </summary>
    /// <param name="organization">The organization whose contacts are listed.</param>
    /// <param name="id">The contact's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the values.</param>
    /// <returns>The values, or <see langword="null"/> when the organization has no contact with the
    /// identifier.</returns>
    public Task<ContactSortKey?> FindSortKeyAsync(
        OrganizationId organization,
        ContactId id,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Contact> contacts = this._context.Set<Contact>();
        return contacts
            .Where(contact => contact.Id == id && contact.OrganizationId == organization)
            .Select(static contact => new ContactSortKey { Id = contact.Id, Name = contact.Name })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Reads an organization's contacts in name order, starting just past a cursor's contact. A page before its cursor
    /// is read backwards from it, nearest first.
    /// </summary>
    /// <remarks>
    /// The organization, the name and the identifier are the columns of one index, in that order, so the database finds
    /// the organization's contacts, and the cursor's place among them, by seeking in it, and reads them in order from
    /// there.
    /// </remarks>
    /// <param name="request">The organization and the cursor to read from.</param>
    /// <param name="cursorKey">How the list sorts the cursor's contact, or <see langword="null"/> to start at the
    /// beginning.</param>
    /// <param name="limit">How many contacts to read at most.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the contacts.</param>
    /// <returns>The contacts, in the order they were read.</returns>
    public Task<ContactSummary[]> ListAsync(
        ContactCollectionRequest request,
        ContactSortKey? cursorKey,
        RecordCount limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OrganizationId organization = request.Organization;
        bool backwards = request.Cursor?.Position == CursorPosition.Before;
        IQueryable<Contact> contacts = this._context.Set<Contact>();
        IQueryable<Contact> matching = contacts.Where(contact => contact.OrganizationId == organization);
        if (cursorKey is not null)
        {
            matching = matching.Where(Beyond(cursorKey, backwards));
        }

        IOrderedQueryable<Contact> sorted = backwards
            ? matching.OrderByDescending(static contact => contact.Name).ThenByDescending(static contact => contact.Id)
            : matching.OrderBy(static contact => contact.Name).ThenBy(static contact => contact.Id);
        return sorted
            .Take(limit.Value)
            .Select(static contact => new ContactSummary
            {
                Id = contact.Id,
                Name = contact.Name,
                JobTitle = contact.JobTitle,
                Email = contact.Email,
                Phone = contact.Phone,
                ArchivedOn = contact.ArchivedOn,
            })
            .ToArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Reads one contact's row to change it. The context keeps track of the row, so a later
    /// <see cref="SaveChangeAsync"/> in the same request writes only what changed.
    /// </summary>
    /// <param name="id">The contact's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the row.</param>
    /// <returns>The row, or <see langword="null"/> when no contact has the identifier.</returns>
    public Task<Contact?> FindForChangeAsync(ContactId id, CancellationToken cancellationToken = default)
    {
        IQueryable<Contact> contacts = this._context.Set<Contact>();
        return contacts.FirstOrDefaultAsync(contact => contact.Id == id, cancellationToken);
    }

    /// <summary>
    /// Says whether an organization is archived, which closes its contacts to changes until it is restored.
    /// </summary>
    /// <param name="organization">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns><see langword="true"/> when the organization is archived.</returns>
    public Task<bool> IsOrganizationArchivedAsync(
        OrganizationId organization,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Organization> organizations = this._context.Set<Organization>();
        return organizations.AnyAsync(
            candidate => candidate.Id == organization && candidate.ArchivedOn != null,
            cancellationToken);
    }

    /// <summary>
    /// Saves a change to a contact: their row's new values, then the version that records them, in one transaction.
    /// </summary>
    /// <remarks>
    /// As for an organization, the row's version number is a concurrency token, so the UPDATE names the version that
    /// was read; if another request saved the contact after this one read them, it matches no row, nothing is written,
    /// and the transaction is rolled back. The row is saved before its version so that this check comes first.
    /// </remarks>
    /// <param name="current">The row as <see cref="FindForChangeAsync"/> read it.</param>
    /// <param name="changed">The row's new values.</param>
    /// <param name="version">The version that records the change.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns><see langword="true"/> when the change was saved; <see langword="false"/> when another save overtook
    /// it.</returns>
    public async Task<bool> SaveChangeAsync(
        Contact current,
        Contact changed,
        ContactVersion version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(changed);
        ArgumentNullException.ThrowIfNull(version);
        DatabaseFacade database = this._context.Database;
        IDbContextTransaction transaction = await database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            EntityEntry<Contact> entry = this._context.Entry(current);
            PropertyValues values = entry.CurrentValues;
            values.SetValues(changed);
            bool updated = await this.TrySaveAsync(cancellationToken).ConfigureAwait(false);
            if (!updated)
            {
                return false;
            }

            this._context.Add(version);
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
    }

    /// <summary>
    /// Keeps the contacts that come after the cursor's in the order the page is read: the seek of key-based paging.
    /// </summary>
    /// <remarks>
    /// As in <see cref="OrganizationSeek"/>, the condition says "at least the key's name, and then past the key", so
    /// that its first half is a range the index can seek to, and the names and identifiers are compared with the
    /// adapter's own operators, <see cref="DisplayNameComparison"/> and <see cref="ContactIdComparison"/>, which
    /// Entity Framework turns into SQL on the bare columns.
    /// </remarks>
    /// <param name="key">How the list sorts the cursor's contact.</param>
    /// <param name="backwards">Whether the page is read backwards, for the page before the cursor.</param>
    /// <returns>The condition.</returns>
    private static Expression<Func<Contact, bool>> Beyond(ContactSortKey key, bool backwards)
    {
        ContactId id = key.Id;
        DisplayName name = key.Name;
        if (backwards)
        {
            return contact => contact.Name <= name && (contact.Name < name || contact.Id < id);
        }

        return contact => contact.Name >= name && (contact.Name > name || contact.Id > id);
    }

    /// <summary>
    /// Saves what the context holds, unless a row it changes was saved by another request since it was read.
    /// </summary>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns><see langword="false"/> when a concurrency token no longer matched.</returns>
    private async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // An expected outcome, not a failure: the caller tells the user, who decides whether to try again.
            return false;
        }
    }
}
