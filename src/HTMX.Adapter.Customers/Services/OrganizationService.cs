namespace HTMX.Adapter.Customers.Services;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.DTOs;
using HTMX.Adapter.Customers.Entities;
using HTMX.Adapter.Customers.Providers;

/// <summary>
/// The rules over organizations, behind <see cref="IOrganizationService"/>; the queries themselves are the provider's.
/// </summary>
internal sealed class OrganizationService : IOrganizationService
{
    private readonly OrganizationProvider _provider;
    private readonly OrganizationVersionProvider _versions;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationService"/> class.
    /// </summary>
    /// <param name="provider">Runs the queries over organizations.</param>
    /// <param name="versions">Runs the queries over their versions.</param>
    /// <param name="clock">Says when a change is made.</param>
    public OrganizationService(
        OrganizationProvider provider,
        OrganizationVersionProvider versions,
        TimeProvider clock)
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
    /// A page is read one organization longer than it shows: if that extra organization is there, so is another page,
    /// and no count is needed to know it. A cursor whose organization no longer exists, or that leaves too little to
    /// show, starts again at the first page, so an old link never shows an empty or a short page.
    /// </remarks>
    public async Task<OrganizationCollectionResponse> ListAsync(
        OrganizationCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RecordCount active = await this._provider
            .CountAsync(request.Filter, RecordStatus.Active, cancellationToken)
            .ConfigureAwait(false);
        RecordCount archived = await this._provider
            .CountAsync(request.Filter, RecordStatus.Archived, cancellationToken)
            .ConfigureAwait(false);

        OrganizationSortKey? key = await this.FindCursorKeyAsync(request.Cursor, cancellationToken).ConfigureAwait(false);
        OrganizationCollectionRequest answered = key is null ? OrganizationPages.FirstPage(request) : request;
        OrganizationSummary[] read = await this.ReadAsync(answered, key, cancellationToken).ConfigureAwait(false);
        if (key is not null && OrganizationPages.IsTooShort(answered, read))
        {
            answered = OrganizationPages.FirstPage(request);
            read = await this.ReadAsync(answered, null, cancellationToken).ConfigureAwait(false);
        }

        return OrganizationPages.CreatePage(answered, read, active, archived);
    }

    /// <inheritdoc/>
    public Task<OrganizationReference[]> ListChoicesAsync(CancellationToken cancellationToken = default)
    {
        return this._provider.ListChoicesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<OrganizationSingleResponse?> FindAsync(
        OrganizationId id,
        CancellationToken cancellationToken = default)
    {
        return this._provider.FindAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<OrganizationVersionResponse[]> ListVersionsAsync(
        OrganizationId id,
        CancellationToken cancellationToken = default)
    {
        return this._versions.ListAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The new organization is at version 1, created and last changed by the same user at the same moment, and its
    /// first version, a Created one, holds the same values. Its identifier carries on from the highest the database has
    /// ever given.
    /// </remarks>
    public async Task<OrganizationCreateResponse> CreateAsync(
        OrganizationCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        bool taken = await this._provider
            .IsNameTakenAsync(request.Name, except: null, cancellationToken)
            .ConfigureAwait(false);
        if (taken)
        {
            return new OrganizationCreateResponse { Outcome = CreateOutcome.NameTaken };
        }

        Organization organization = Start(request, this._clock.GetUtcNow().UtcDateTime);
        OrganizationId id = await this._provider
            .AddAsync(organization, RecordCreation, cancellationToken)
            .ConfigureAwait(false);
        return new OrganizationCreateResponse { Outcome = CreateOutcome.Created, Id = id };
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The rules run in order, and the first that applies is the answer: the organization must exist, be active, still
    /// be at the version the form was filled from, hold some value other than the ones sent, and keep a name no other
    /// organization has. A change that passes is saved as the next version, and the row takes that version's values,
    /// as the seed builds every row from its latest version.
    /// </remarks>
    public Task<UpdateOutcome> UpdateAsync(
        OrganizationUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        return this.SaveValuesAsync(request, rolledBackTo: null, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A roll-back is a change whose values come from an earlier version, so it runs the same rules as
    /// <see cref="UpdateAsync"/>, and is recorded as a RolledBack version that names the version it went back to.
    /// History is never rewritten: rolling back adds a version, and rolling back again undoes it.
    /// </remarks>
    public async Task<UpdateOutcome> RollBackAsync(
        OrganizationRollBackRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OrganizationVersion? earlier = await this._versions
            .FindAsync(request.Id, request.To, cancellationToken)
            .ConfigureAwait(false);
        if (earlier is null)
        {
            return UpdateOutcome.NotFound;
        }

        OrganizationUpdateRequest values = new OrganizationUpdateRequest
        {
            Id = request.Id,
            Version = request.Version,
            Name = earlier.Name,
            Industry = earlier.Industry,
            Website = earlier.Website,
            ChangedBy = request.ChangedBy,
        };
        return await this.SaveValuesAsync(values, request.To, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> ArchiveAsync(
        OrganizationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        return this.ChangeStatusAsync(request, ChangeKind.Archived, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> RestoreAsync(
        OrganizationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        return this.ChangeStatusAsync(request, ChangeKind.Restored, cancellationToken);
    }

    /// <summary>
    /// Gives the outcome a change has before anything is saved: refused because the organization is archived or has
    /// moved on since the form was filled, or done already because it holds the values sent.
    /// </summary>
    /// <param name="current">The organization as it is now.</param>
    /// <param name="request">The change.</param>
    /// <returns>The outcome, or <see langword="null"/> when the change can go on to be saved.</returns>
    private static UpdateOutcome? OutcomeWithoutSaving(Organization current, OrganizationUpdateRequest request)
    {
        if (current.ArchivedOn is not null)
        {
            return UpdateOutcome.Archived;
        }

        if (current.Version != request.Version)
        {
            return UpdateOutcome.Overtaken;
        }

        bool same = current.Name == request.Name
            && current.Industry == request.Industry
            && current.Website == request.Website;
        return same ? UpdateOutcome.Unchanged : null;
    }

    /// <summary>
    /// Makes a new organization's row: the values sent, at version 1, created and last changed by whoever sent them.
    /// </summary>
    /// <param name="request">The new organization.</param>
    /// <param name="now">When it is created, in UTC.</param>
    /// <returns>The row, without an identifier until the database gives it one.</returns>
    private static Organization Start(OrganizationCreateRequest request, DateTime now)
    {
        return new Organization
        {
            Name = request.Name,
            Industry = request.Industry,
            Website = request.Website,
            CreatedOn = now,
            CreatedBy = request.CreatedBy,
            LastModifiedOn = now,
            LastModifiedBy = request.CreatedBy,
            Version = new VersionNumber(1),
        };
    }

    /// <summary>
    /// Makes the first version of a new organization from its row as saved: a Created version holding the same values.
    /// </summary>
    /// <param name="organization">The row, with the identifier the database gave it.</param>
    /// <returns>The version.</returns>
    private static OrganizationVersion RecordCreation(Organization organization)
    {
        return new OrganizationVersion
        {
            OrganizationId = organization.Id,
            Version = organization.Version,
            Change = ChangeKind.Created,
            ChangedOn = organization.CreatedOn,
            ChangedBy = organization.CreatedBy,
            Name = organization.Name,
            Industry = organization.Industry,
            Website = organization.Website,
        };
    }

    /// <summary>
    /// Makes the version that records a change: the next number, the values sent, and who saved them and when.
    /// </summary>
    /// <param name="current">The organization as it is now, which is active.</param>
    /// <param name="request">The change.</param>
    /// <param name="rolledBackTo">The earlier version the values came from, or <see langword="null"/> for an
    /// edit.</param>
    /// <param name="now">When the change is saved, in UTC.</param>
    /// <returns>The version.</returns>
    private static OrganizationVersion Record(
        Organization current,
        OrganizationUpdateRequest request,
        VersionNumber? rolledBackTo,
        DateTime now)
    {
        return new OrganizationVersion
        {
            OrganizationId = current.Id,
            Version = new VersionNumber(current.Version.Value + 1),
            Change = rolledBackTo is null ? ChangeKind.Edited : ChangeKind.RolledBack,
            RolledBackTo = rolledBackTo,
            ChangedOn = now,
            ChangedBy = request.ChangedBy,
            Name = request.Name,
            Industry = request.Industry,
            Website = request.Website,
        };
    }

    /// <summary>
    /// Makes the version that records an organization being archived or restored: the next number, its values as they
    /// are, and when and by whom it was archived, or nothing there once it is restored.
    /// </summary>
    /// <param name="current">The organization as it is now.</param>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="by">Who is archiving or restoring it.</param>
    /// <param name="now">When, in UTC.</param>
    /// <returns>The version.</returns>
    private static OrganizationVersion RecordStatusChange(
        Organization current,
        ChangeKind change,
        UserId by,
        DateTime now)
    {
        bool archiving = change == ChangeKind.Archived;
        return new OrganizationVersion
        {
            OrganizationId = current.Id,
            Version = new VersionNumber(current.Version.Value + 1),
            Change = change,
            ChangedOn = now,
            ChangedBy = by,
            Name = current.Name,
            Industry = current.Industry,
            Website = current.Website,
            ArchivedOn = archiving ? now : null,
            ArchivedBy = archiving ? by : null,
        };
    }

    /// <summary>
    /// Gives an organization's row the values of its newest version, keeping who created it and when.
    /// </summary>
    /// <param name="current">The row as it is now.</param>
    /// <param name="version">The newest version.</param>
    /// <returns>The row's new values.</returns>
    private static Organization Apply(Organization current, OrganizationVersion version)
    {
        // Written out rather than with a with expression, which StyleCop 1.1.118 misreads in an instance class (SA1101).
        return new Organization
        {
            Id = current.Id,
            Name = version.Name,
            Industry = version.Industry,
            Website = version.Website,
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
    /// Reads how the list sorts the organization a cursor names.
    /// </summary>
    /// <param name="cursor">The cursor, or <see langword="null"/> for the first page.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the values.</param>
    /// <returns>The values, or <see langword="null"/> for no cursor or an organization that no longer exists.</returns>
    private async Task<OrganizationSortKey?> FindCursorKeyAsync(
        OrganizationCursor? cursor,
        CancellationToken cancellationToken)
    {
        if (cursor is not OrganizationCursor named)
        {
            return null;
        }

        return await this._provider.FindSortKeyAsync(named.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads one organization more than a page holds, starting just past the cursor's organization.
    /// </summary>
    /// <param name="request">The page to read.</param>
    /// <param name="key">How the list sorts the cursor's organization, or <see langword="null"/> for the first
    /// page.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the organizations.</param>
    /// <returns>The organizations, in the order they were read.</returns>
    private Task<OrganizationSummary[]> ReadAsync(
        OrganizationCollectionRequest request,
        OrganizationSortKey? key,
        CancellationToken cancellationToken)
    {
        RecordCount limit = new RecordCount(request.PageSize.Value + 1);
        return this._provider.ListAsync(request, key, limit, cancellationToken);
    }

    /// <summary>
    /// Archives or restores an organization as its next version. An organization already archived, or already active,
    /// is left as it is.
    /// </summary>
    /// <param name="request">Which organization, and who is asking.</param>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>What became of the request.</returns>
    private async Task<UpdateOutcome> ChangeStatusAsync(
        OrganizationStatusRequest request,
        ChangeKind change,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Organization? current = await this._provider
            .FindForChangeAsync(request.Id, cancellationToken)
            .ConfigureAwait(false);
        if (current is null)
        {
            return UpdateOutcome.NotFound;
        }

        bool archived = current.ArchivedOn is not null;
        if (archived == (change == ChangeKind.Archived))
        {
            return UpdateOutcome.Unchanged;
        }

        DateTime now = this._clock.GetUtcNow().UtcDateTime;
        OrganizationVersion version = RecordStatusChange(current, change, request.ChangedBy, now);
        Organization changed = Apply(current, version);
        bool saved = await this._provider
            .SaveChangeAsync(current, changed, version, cancellationToken)
            .ConfigureAwait(false);
        return saved ? UpdateOutcome.Saved : UpdateOutcome.Overtaken;
    }

    /// <summary>
    /// Saves new values for an organization as its next version, after the rules, in order: the organization must
    /// exist, be active, still be at the version the values started from, hold some value other than the ones sent,
    /// and keep a name no other organization has. The row takes the version's values, as the seed builds every row
    /// from its latest version.
    /// </summary>
    /// <param name="request">The values, the version they started from, and who is saving them.</param>
    /// <param name="rolledBackTo">The earlier version the values came from, or <see langword="null"/> for an
    /// edit.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Whether the values were saved and, if not, why.</returns>
    private async Task<UpdateOutcome> SaveValuesAsync(
        OrganizationUpdateRequest request,
        VersionNumber? rolledBackTo,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Organization? current = await this._provider
            .FindForChangeAsync(request.Id, cancellationToken)
            .ConfigureAwait(false);
        if (current is null)
        {
            return UpdateOutcome.NotFound;
        }

        if (OutcomeWithoutSaving(current, request) is UpdateOutcome settled)
        {
            return settled;
        }

        bool taken = await this._provider
            .IsNameTakenAsync(request.Name, request.Id, cancellationToken)
            .ConfigureAwait(false);
        if (taken)
        {
            return UpdateOutcome.NameTaken;
        }

        DateTime now = this._clock.GetUtcNow().UtcDateTime;
        OrganizationVersion version = Record(current, request, rolledBackTo, now);
        Organization changed = Apply(current, version);
        bool saved = await this._provider
            .SaveChangeAsync(current, changed, version, cancellationToken)
            .ConfigureAwait(false);
        return saved ? UpdateOutcome.Saved : UpdateOutcome.Overtaken;
    }
}
