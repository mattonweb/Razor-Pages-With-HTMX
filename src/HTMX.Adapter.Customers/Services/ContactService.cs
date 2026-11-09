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
        if (key is not null && ContactPages.IsTooShort(answered, read))
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
    /// The rules are the ones a change to a contact's values runs, in order, and the first that applies is the answer.
    /// A roll-back is recorded as a RolledBack version that names the version it went back to; history is never
    /// rewritten, and rolling back again undoes it. The organization is one of the values, so a roll-back can move the
    /// contact back to an organization they left, as long as it still takes people in.
    /// </remarks>
    public async Task<UpdateOutcome> RollBackAsync(
        ContactRollBackRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ContactVersion? earlier = await this._versions
            .FindAsync(request.Id, request.To, cancellationToken)
            .ConfigureAwait(false);
        Contact? current = await this._provider.FindForChangeAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (earlier is null || current is null)
        {
            return UpdateOutcome.NotFound;
        }

        UpdateOutcome? settled = await this
            .OutcomeWithoutSavingAsync(current, earlier, request.Version, cancellationToken)
            .ConfigureAwait(false);
        if (settled is UpdateOutcome refused)
        {
            return refused;
        }

        DateTime now = this._clock.GetUtcNow().UtcDateTime;
        ContactVersion version = RecordRollBack(current, earlier, request.ChangedBy, now);
        Contact changed = Apply(current, version);
        bool saved = await this._provider
            .SaveChangeAsync(current, changed, version, cancellationToken)
            .ConfigureAwait(false);
        return saved ? UpdateOutcome.Saved : UpdateOutcome.Overtaken;
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
    /// Makes the version that records a roll-back: the next number, the earlier version's values, active, and who
    /// rolled back and when.
    /// </summary>
    /// <param name="current">The contact as they are now, who is active.</param>
    /// <param name="earlier">The version whose values are saved again.</param>
    /// <param name="by">Who is rolling the contact back.</param>
    /// <param name="now">When, in UTC.</param>
    /// <returns>The version.</returns>
    private static ContactVersion RecordRollBack(Contact current, ContactVersion earlier, UserId by, DateTime now)
    {
        return new ContactVersion
        {
            ContactId = current.Id,
            Version = new VersionNumber(current.Version.Value + 1),
            Change = ChangeKind.RolledBack,
            RolledBackTo = earlier.Version,
            ChangedOn = now,
            ChangedBy = by,
            Name = earlier.Name,
            Email = earlier.Email,
            Phone = earlier.Phone,
            JobTitle = earlier.JobTitle,
            OrganizationId = earlier.OrganizationId,
            Notes = earlier.Notes,
        };
    }

    /// <summary>
    /// Says whether a contact already holds every value a version holds.
    /// </summary>
    /// <param name="current">The contact as they are now.</param>
    /// <param name="version">The version.</param>
    /// <returns><see langword="true"/> when nothing would change.</returns>
    private static bool HoldsTheValuesOf(Contact current, ContactVersion version)
    {
        return current.Name == version.Name
            && current.Email == version.Email
            && current.Phone == version.Phone
            && current.JobTitle == version.JobTitle
            && current.OrganizationId == version.OrganizationId
            && current.Notes == version.Notes;
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
    /// Gives the outcome a roll-back has before anything is saved: refused because the contact or their organization
    /// is archived, because the contact has moved on since their history was shown, or because the organization the
    /// earlier version names no longer takes people in; or done already because the contact holds those values.
    /// </summary>
    /// <param name="current">The contact as they are now.</param>
    /// <param name="earlier">The version to roll back to.</param>
    /// <param name="expected">The version the request says the contact is at.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>The outcome, or <see langword="null"/> when the roll-back can go on to be saved.</returns>
    private async Task<UpdateOutcome?> OutcomeWithoutSavingAsync(
        Contact current,
        ContactVersion earlier,
        VersionNumber expected,
        CancellationToken cancellationToken)
    {
        bool open = current.ArchivedOn is null && await this._provider
            .IsOrganizationActiveAsync(current.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (!open)
        {
            return UpdateOutcome.Archived;
        }

        if (current.Version != expected)
        {
            return UpdateOutcome.Overtaken;
        }

        if (HoldsTheValuesOf(current, earlier))
        {
            return UpdateOutcome.Unchanged;
        }

        bool moving = earlier.OrganizationId != current.OrganizationId;
        bool welcome = !moving || await this._provider
            .IsOrganizationActiveAsync(earlier.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        return welcome ? null : UpdateOutcome.OrganizationUnavailable;
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
        ContactVersion version = RecordStatusChange(current, change, request.ChangedBy, now);
        Contact changed = Apply(current, version);
        bool saved = await this._provider
            .SaveChangeAsync(current, changed, version, cancellationToken)
            .ConfigureAwait(false);
        return saved ? UpdateOutcome.Saved : UpdateOutcome.Overtaken;
    }
}
