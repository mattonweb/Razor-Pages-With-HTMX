namespace HTMX.Test.Unit.Application.Web.Pages.Organizations;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Stands in for the organization service in page tests: it keeps each request the page sends and answers with an
/// empty page for a list, with the one organization it was given for a lookup, and with the outcome it was given for a
/// change, so a test sees what the page asked for without a database.
/// </summary>
internal sealed class StubOrganizationService : IOrganizationService
{
    private readonly List<OrganizationCollectionRequest> _received = [];
    private readonly List<OrganizationId> _looked = [];
    private readonly List<OrganizationUpdateRequest> _updates = [];
    private readonly OrganizationSingleResponse? _found;
    private readonly UpdateOutcome _outcome;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubOrganizationService"/> class.
    /// </summary>
    /// <param name="found">The organization a lookup answers with, or <see langword="null"/> for none.</param>
    /// <param name="outcome">What a change is answered with.</param>
    public StubOrganizationService(OrganizationSingleResponse? found = null, UpdateOutcome outcome = UpdateOutcome.Saved)
    {
        this._found = found;
        this._outcome = outcome;
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
}
