namespace HTMX.Adapter.Customers.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;
using HTMX.Adapter.Customers.Providers;

/// <summary>
/// The rules over contacts, behind <see cref="IContactService"/>; the queries themselves are the provider's.
/// </summary>
internal sealed class ContactService : IContactService
{
    private readonly ContactProvider _provider;
    private readonly ContactVersionProvider _versions;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactService"/> class.
    /// </summary>
    /// <param name="provider">Runs the queries over contacts.</param>
    /// <param name="versions">Runs the queries over their versions.</param>
    /// <param name="clock">Says when a change is made.</param>
    public ContactService(ContactProvider provider, ContactVersionProvider versions, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(versions);
        ArgumentNullException.ThrowIfNull(clock);
        this._provider = provider;
        this._versions = versions;
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
        if (key is not null && ListPages.IsTooShort(answered.Cursor?.Position, answered.PageSize, read))
        {
            answered = ContactPages.FirstPage(request);
            read = await this.ReadAsync(answered, null, cancellationToken).ConfigureAwait(false);
        }

        return ContactPages.CreatePage(answered, read, total);
    }

    /// <inheritdoc/>
    public Task<ContactSingleResponse?> FindAsync(ContactId id, CancellationToken cancellationToken = default)
    {
        return this._provider.FindAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ContactVersionResponse[]> ListVersionsAsync(
        ContactId id,
        CancellationToken cancellationToken = default)
    {
        return this._versions.ListAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The new contact is at version 1, created and last changed by the same user at the same moment, and their first
    /// version, a Created one, holds the same values. Their identifier carries on from the highest the database has
    /// ever given.
    /// </remarks>
    public async Task<ContactCreateResponse> CreateAsync(
        ContactCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        bool welcome = await this._provider
            .IsOrganizationActiveAsync(request.Organization, cancellationToken)
            .ConfigureAwait(false);
        if (!welcome)
        {
            return new ContactCreateResponse { Outcome = CreateOutcome.OrganizationUnavailable };
        }

        Contact contact = ContactHistory.Start(request, this._clock.GetUtcNow().UtcDateTime);
        ContactId id = await this._provider
            .AddAsync(contact, ContactHistory.RecordCreation, cancellationToken)
            .ConfigureAwait(false);
        return new ContactCreateResponse { Outcome = CreateOutcome.Created, Id = id };
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The rules run in order, and the first that applies is the answer: the contact must exist, they and their
    /// organization must be active, they must still be at the version the form was filled from, hold some value other
    /// than the ones sent, and the organization the change names, when it is another, must take people in. A change
    /// that passes is saved as the next version, and the row takes that version's values.
    /// </remarks>
    public Task<UpdateOutcome> UpdateAsync(
        ContactUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        return this.SaveValuesAsync(request, rolledBackTo: null, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A roll-back is a change whose values come from an earlier version, so it runs the same rules as
    /// <see cref="UpdateAsync"/>, and is recorded as a RolledBack version that names the version it went back to;
    /// history is never rewritten, and rolling back again undoes it. The organization is one of the values, so a
    /// roll-back can move the contact back to an organization they left, as long as it still takes people in.
    /// </remarks>
    public async Task<UpdateOutcome> RollBackAsync(
        ContactRollBackRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ContactVersion? earlier = await this._versions
            .FindAsync(request.Id, request.To, cancellationToken)
            .ConfigureAwait(false);
        if (earlier is null)
        {
            return UpdateOutcome.NotFound;
        }

        ContactUpdateRequest values = new ContactUpdateRequest
        {
            Id = request.Id,
            Version = request.Version,
            Name = earlier.Name,
            JobTitle = earlier.JobTitle,
            Email = earlier.Email,
            Phone = earlier.Phone,
            Notes = earlier.Notes,
            Organization = earlier.OrganizationId,
            ChangedBy = request.ChangedBy,
        };
        return await this.SaveValuesAsync(values, request.To, cancellationToken).ConfigureAwait(false);
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

    /// <inheritdoc/>
    /// <remarks>
    /// Only an archived contact can be deleted, so that nothing is deleted in one step, and only while their
    /// organization is active: an archived organization's contacts are closed to changes until it is restored, and a
    /// delete is one. Nothing but their own history names a contact, so nothing else keeps one from being deleted.
    /// </remarks>
    public async Task<DeleteOutcome> DeleteAsync(
        ContactDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Contact? current = await this._provider.FindForChangeAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return DeleteOutcome.NotFound;
        }

        bool open = await this._provider
            .IsOrganizationActiveAsync(current.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (!open)
        {
            return DeleteOutcome.OrganizationArchived;
        }

        if (current.ArchivedOn is null)
        {
            return DeleteOutcome.Active;
        }

        if (current.Version != request.Version)
        {
            return DeleteOutcome.Overtaken;
        }

        bool deleted = await this._provider.DeleteAsync(current, cancellationToken).ConfigureAwait(false);
        return deleted ? DeleteOutcome.Deleted : DeleteOutcome.Overtaken;
    }

    /// <summary>
    /// Says whether a contact already holds every value a change sends.
    /// </summary>
    /// <param name="current">The contact as they are now.</param>
    /// <param name="request">The change.</param>
    /// <returns><see langword="true"/> when nothing would change.</returns>
    private static bool HoldsTheValuesOf(Contact current, ContactUpdateRequest request)
    {
        return current.Name == request.Name
            && current.Email == request.Email
            && current.Phone == request.Phone
            && current.JobTitle == request.JobTitle
            && current.OrganizationId == request.Organization
            && current.Notes == request.Notes;
    }

    /// <summary>
    /// Gives the outcome a change has before anything is saved: refused because the contact or their organization is
    /// archived, because the contact has moved on since the form was filled, or because the organization the change
    /// names takes no one in; or done already because the contact holds the values sent.
    /// </summary>
    /// <param name="current">The contact as they are now.</param>
    /// <param name="request">The change.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>The outcome, or <see langword="null"/> when the change can go on to be saved.</returns>
    private async Task<UpdateOutcome?> OutcomeWithoutSavingAsync(
        Contact current,
        ContactUpdateRequest request,
        CancellationToken cancellationToken)
    {
        bool open = current.ArchivedOn is null && await this._provider
            .IsOrganizationActiveAsync(current.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (!open)
        {
            return UpdateOutcome.Archived;
        }

        if (current.Version != request.Version)
        {
            return UpdateOutcome.Overtaken;
        }

        if (HoldsTheValuesOf(current, request))
        {
            return UpdateOutcome.Unchanged;
        }

        bool moving = request.Organization != current.OrganizationId;
        bool welcome = !moving || await this._provider
            .IsOrganizationActiveAsync(request.Organization, cancellationToken)
            .ConfigureAwait(false);
        return welcome ? null : UpdateOutcome.OrganizationUnavailable;
    }

    /// <summary>
    /// Saves new values for a contact as their next version, after the rules, in order: the contact must exist, they
    /// and their organization must be active, they must still be at the version the values started from, hold some
    /// value other than the ones sent, and the organization the values name must take people in.
    /// </summary>
    /// <param name="request">The values, the version they started from, and who is saving them.</param>
    /// <param name="rolledBackTo">The earlier version the values came from, or <see langword="null"/> for an
    /// edit.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Whether the values were saved and, if not, why.</returns>
    private async Task<UpdateOutcome> SaveValuesAsync(
        ContactUpdateRequest request,
        VersionNumber? rolledBackTo,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Contact? current = await this._provider.FindForChangeAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return UpdateOutcome.NotFound;
        }

        UpdateOutcome? settled = await this
            .OutcomeWithoutSavingAsync(current, request, cancellationToken)
            .ConfigureAwait(false);
        if (settled is UpdateOutcome refused)
        {
            return refused;
        }

        DateTime now = this._clock.GetUtcNow().UtcDateTime;
        ContactVersion version = ContactHistory.Record(current, request, rolledBackTo, now);
        Contact changed = ContactHistory.Apply(current, version);
        bool saved = await this._provider
            .SaveChangeAsync(current, changed, version, cancellationToken)
            .ConfigureAwait(false);
        return saved ? UpdateOutcome.Saved : UpdateOutcome.Overtaken;
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
    /// Archives or restores a contact as their next version, after the rules, in order: the contact must exist and,
    /// when the request names an organization, belong to it; their organization must be active; a contact already
    /// archived, or already active, is left as they are.
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
        bool elsewhere = request.Organization is OrganizationId named && current?.OrganizationId != named;
        if (current is null || elsewhere)
        {
            return UpdateOutcome.NotFound;
        }

        bool open = await this._provider
            .IsOrganizationActiveAsync(current.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (!open)
        {
            return UpdateOutcome.Archived;
        }

        bool archived = current.ArchivedOn is not null;
        if (archived == (change == ChangeKind.Archived))
        {
            return UpdateOutcome.Unchanged;
        }

        DateTime now = this._clock.GetUtcNow().UtcDateTime;
        ContactVersion version = ContactHistory.RecordStatusChange(current, change, request.ChangedBy, now);
        Contact changed = ContactHistory.Apply(current, version);
        bool saved = await this._provider
            .SaveChangeAsync(current, changed, version, cancellationToken)
            .ConfigureAwait(false);
        return saved ? UpdateOutcome.Saved : UpdateOutcome.Overtaken;
    }
}
