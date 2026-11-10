namespace HTMX.Test.Unit.Application.Web.Pages;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Stands in for the organization service in page tests: it keeps each request the page sends and answers with an
/// empty page for a list, with the one organization, the versions and the choices it was given for a lookup, and with
/// the outcomes it was given for a change, an archive or restore, a roll-back, a new organization and a delete, so a
/// test sees what the page asked for without a database.
/// </summary>
internal sealed class StubOrganizationService : IOrganizationService
{
    private readonly List<OrganizationCollectionRequest> _received = [];
    private readonly List<OrganizationId> _looked = [];
    private readonly List<OrganizationUpdateRequest> _updates = [];
    private readonly List<OrganizationCreateRequest> _creations = [];
    private readonly List<(ChangeKind Change, OrganizationStatusRequest Request)> _statusChanges = [];
    private readonly List<OrganizationRollBackRequest> _rollBacks = [];
    private readonly List<OrganizationDeleteRequest> _deletions = [];
    private readonly OrganizationSingleResponse? _found;
    private readonly UpdateOutcome _outcome;
    private readonly CreateOutcome _creation;
    private readonly OrganizationVersionResponse[] _versions;
    private readonly OrganizationReference[] _choices;
    private readonly DeleteOutcome _deletion;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubOrganizationService"/> class.
    /// </summary>
    /// <param name="found">The organization a lookup answers with, or <see langword="null"/> for none.</param>
    /// <param name="outcome">What a change, an archive, a restore and a roll-back are answered with.</param>
    /// <param name="creation">What a new organization is answered with; one that is created gets the identifier
    /// 11001, as the first after the seed would.</param>
    /// <param name="versions">The versions a lookup of the history answers with, newest first.</param>
    /// <param name="choices">The organizations a list of choices answers with.</param>
    /// <param name="deletion">What a delete is answered with.</param>
    public StubOrganizationService(
        OrganizationSingleResponse? found = null,
        UpdateOutcome outcome = UpdateOutcome.Saved,
        CreateOutcome creation = CreateOutcome.Created,
        OrganizationVersionResponse[]? versions = null,
        OrganizationReference[]? choices = null,
        DeleteOutcome deletion = DeleteOutcome.Deleted)
    {
        this._found = found;
        this._outcome = outcome;
        this._creation = creation;
        this._versions = versions ?? [];
        this._choices = choices ?? [];
        this._deletion = deletion;
    }

    /// <summary>
    /// Gives the last request the page sent.
    /// </summary>
    /// <returns>The request, or <see langword="null"/> before the page sends one.</returns>
    public OrganizationCollectionRequest? LastRequest()
    {
        return this._received.LastOrDefault();
    }

    /// <summary>
    /// Gives the identifier of the last organization the page looked up.
    /// </summary>
    /// <returns>The identifier, or <see langword="null"/> before the page looks one up.</returns>
    public OrganizationId? LastLookup()
    {
        return this._looked.Count == 0 ? null : this._looked[^1];
    }

    /// <summary>
    /// Gives the last change the page sent.
    /// </summary>
    /// <returns>The change, or <see langword="null"/> before the page sends one.</returns>
    public OrganizationUpdateRequest? LastUpdate()
    {
        return this._updates.LastOrDefault();
    }

    /// <summary>
    /// Gives the last new organization the page sent.
    /// </summary>
    /// <returns>The new organization, or <see langword="null"/> before the page sends one.</returns>
    public OrganizationCreateRequest? LastCreation()
    {
        return this._creations.LastOrDefault();
    }

    /// <summary>
    /// Gives the last archive or restore the page asked for.
    /// </summary>
    /// <returns>Archived or Restored, with the request, or <see langword="null"/> before the page asks for
    /// either.</returns>
    public (ChangeKind Change, OrganizationStatusRequest Request)? LastStatusChange()
    {
        return this._statusChanges.Count == 0 ? null : this._statusChanges[^1];
    }

    /// <summary>
    /// Gives the last delete the page asked for.
    /// </summary>
    /// <returns>The delete, or <see langword="null"/> before the page asks for one.</returns>
    public OrganizationDeleteRequest? LastDeletion()
    {
        return this._deletions.LastOrDefault();
    }

    /// <summary>
    /// Gives the last roll-back the page asked for.
    /// </summary>
    /// <returns>The roll-back, or <see langword="null"/> before the page asks for one.</returns>
    public OrganizationRollBackRequest? LastRollBack()
    {
        return this._rollBacks.LastOrDefault();
    }

    /// <inheritdoc/>
    public Task<OrganizationCollectionResponse> ListAsync(
        OrganizationCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        this._received.Add(request);
        OrganizationCollectionResponse response = new OrganizationCollectionResponse
        {
            Request = request,
            Items = [],
            Matching = new RecordCount(0),
            Active = new RecordCount(0),
            Archived = new RecordCount(0),
            All = new RecordCount(0),
        };
        return Task.FromResult(response);
    }

    /// <inheritdoc/>
    public Task<OrganizationReference[]> ListChoicesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this._choices);
    }

    /// <inheritdoc/>
    public Task<OrganizationSingleResponse?> FindAsync(
        OrganizationId id,
        CancellationToken cancellationToken = default)
    {
        this._looked.Add(id);
        return Task.FromResult(this._found);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> UpdateAsync(
        OrganizationUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        this._updates.Add(request);
        return Task.FromResult(this._outcome);
    }

    /// <inheritdoc/>
    public Task<OrganizationVersionResponse[]> ListVersionsAsync(
        OrganizationId id,
        CancellationToken cancellationToken = default)
    {
        this._looked.Add(id);
        return Task.FromResult(this._versions);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> RollBackAsync(
        OrganizationRollBackRequest request,
        CancellationToken cancellationToken = default)
    {
        this._rollBacks.Add(request);
        return Task.FromResult(this._outcome);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> ArchiveAsync(
        OrganizationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        this._statusChanges.Add((ChangeKind.Archived, request));
        return Task.FromResult(this._outcome);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> RestoreAsync(
        OrganizationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        this._statusChanges.Add((ChangeKind.Restored, request));
        return Task.FromResult(this._outcome);
    }

    /// <inheritdoc/>
    public Task<DeleteOutcome> DeleteAsync(
        OrganizationDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        this._deletions.Add(request);
        return Task.FromResult(this._deletion);
    }

    /// <inheritdoc/>
    public Task<OrganizationCreateResponse> CreateAsync(
        OrganizationCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        this._creations.Add(request);
        OrganizationCreateResponse response = new OrganizationCreateResponse
        {
            Outcome = this._creation,
            Id = this._creation == CreateOutcome.Created ? new OrganizationId(11001) : null,
        };
        return Task.FromResult(response);
    }
}
