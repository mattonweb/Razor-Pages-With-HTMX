namespace HTMX.Adapter.Customers.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.DTOs;
using HTMX.Adapter.Customers.Entities;
using HTMX.Adapter.Customers.Providers;

/// <summary>
/// The rules over contacts, behind <see cref="IContactService"/>; the queries themselves are the provider's.
/// </summary>
internal sealed class ContactService : IContactService
{
    private readonly ContactProvider _provider;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactService"/> class.
    /// </summary>
    /// <param name="provider">Runs the queries over contacts.</param>
    /// <param name="clock">Says when a change is made.</param>
    public ContactService(ContactProvider provider, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(clock);
        this._provider = provider;
        this._clock = clock;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// As for the organizations list, a page is read one contact longer than it shows, and a cursor whose contact the
    /// organization no longer has, or that leaves too little to show, starts again at the first page.
    /// </remarks>
    public async Task<ContactCollectionResponse> ListAsync(
        ContactCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RecordCount total = await this._provider
            .CountAsync(request.Organization, cancellationToken)
            .ConfigureAwait(false);

        ContactSortKey? key = await this.FindCursorKeyAsync(request, cancellationToken).ConfigureAwait(false);
        ContactCollectionRequest answered = key is null ? ContactPages.FirstPage(request) : request;
        ContactSummary[] read = await this.ReadAsync(answered, key, cancellationToken).ConfigureAwait(false);
        if (key is not null && ContactPages.IsTooShort(answered, read))
        {
            answered = ContactPages.FirstPage(request);
            read = await this.ReadAsync(answered, null, cancellationToken).ConfigureAwait(false);
        }

        return ContactPages.CreatePage(answered, read, total);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> ArchiveAsync(
        ContactStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        return this.ChangeStatusAsync(request, ChangeKind.Archived, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> RestoreAsync(
        ContactStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        return this.ChangeStatusAsync(request, ChangeKind.Restored, cancellationToken);
    }

    /// <summary>
    /// Makes the version that records a contact being archived or restored: the next number, their values as they
    /// are, and when and by whom they were archived, or nothing there once they are restored.
    /// </summary>
    /// <param name="current">The contact as they are now.</param>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="by">Who is archiving or restoring them.</param>
    /// <param name="now">When, in UTC.</param>
    /// <returns>The version.</returns>
    private static ContactVersion RecordStatusChange(Contact current, ChangeKind change, UserId by, DateTime now)
    {
        bool archiving = change == ChangeKind.Archived;
        return new ContactVersion
        {
            ContactId = current.Id,
            Version = new VersionNumber(current.Version.Value + 1),
            Change = change,
            ChangedOn = now,
            ChangedBy = by,
            Name = current.Name,
            Email = current.Email,
            Phone = current.Phone,
            JobTitle = current.JobTitle,
            OrganizationId = current.OrganizationId,
            Notes = current.Notes,
            ArchivedOn = archiving ? now : null,
            ArchivedBy = archiving ? by : null,
        };
    }

    /// <summary>
    /// Gives a contact's row the values of their newest version, keeping who created them and when.
    /// </summary>
    /// <param name="current">The row as it is now.</param>
    /// <param name="version">The newest version.</param>
    /// <returns>The row's new values.</returns>
    private static Contact Apply(Contact current, ContactVersion version)
    {
        // Written out rather than with a with expression, which StyleCop 1.1.118 misreads in an instance class
        // (SA1101).
        return new Contact
        {
            Id = current.Id,
            Name = version.Name,
            Email = version.Email,
            Phone = version.Phone,
            JobTitle = version.JobTitle,
            OrganizationId = version.OrganizationId,
            Notes = version.Notes,
            CreatedOn = current.CreatedOn,
            CreatedBy = current.CreatedBy,
            LastModifiedOn = version.ChangedOn,
            LastModifiedBy = version.ChangedBy,
            ArchivedOn = version.ArchivedOn,
            ArchivedBy = version.ArchivedBy,
            Version = version.Version,
        };
    }

    /// <summary>
    /// Reads how the list sorts the contact the request's cursor names.
    /// </summary>
    /// <param name="request">The request, whose cursor may name a contact.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the values.</param>
    /// <returns>The values, or <see langword="null"/> for no cursor or a contact the organization no longer
    /// has.</returns>
    private async Task<ContactSortKey?> FindCursorKeyAsync(
        ContactCollectionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Cursor is not ContactCursor named)
        {
            return null;
        }

        return await this._provider
            .FindSortKeyAsync(request.Organization, named.Id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Reads one contact more than a page holds, starting just past the cursor's contact.
    /// </summary>
    /// <param name="request">The page to read.</param>
    /// <param name="key">How the list sorts the cursor's contact, or <see langword="null"/> for the first page.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the contacts.</param>
    /// <returns>The contacts, in the order they were read.</returns>
    private Task<ContactSummary[]> ReadAsync(
        ContactCollectionRequest request,
        ContactSortKey? key,
        CancellationToken cancellationToken)
    {
        RecordCount limit = new RecordCount(request.PageSize.Value + 1);
        return this._provider.ListAsync(request, key, limit, cancellationToken);
    }

    /// <summary>
    /// Archives or restores a contact as their next version, after the rules, in order: the organization the request
    /// names must have the contact, and must be active; a contact already archived, or already active, is left as
    /// they are.
    /// </summary>
    /// <param name="request">Which contact, their organization, and who is asking.</param>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>What became of the request.</returns>
    private async Task<UpdateOutcome> ChangeStatusAsync(
        ContactStatusRequest request,
        ChangeKind change,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Contact? current = await this._provider.FindForChangeAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (current is null || current.OrganizationId != request.Organization)
        {
            return UpdateOutcome.NotFound;
        }

        bool closed = await this._provider
            .IsOrganizationArchivedAsync(current.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (closed)
        {
            return UpdateOutcome.Archived;
        }

        bool archived = current.ArchivedOn is not null;
        if (archived == (change == ChangeKind.Archived))
        {
            return UpdateOutcome.Unchanged;
        }

        DateTime now = this._clock.GetUtcNow().UtcDateTime;
        ContactVersion version = RecordStatusChange(current, change, request.ChangedBy, now);
        Contact changed = Apply(current, version);
        bool saved = await this._provider
            .SaveChangeAsync(current, changed, version, cancellationToken)
            .ConfigureAwait(false);
        return saved ? UpdateOutcome.Saved : UpdateOutcome.Overtaken;
    }
}
