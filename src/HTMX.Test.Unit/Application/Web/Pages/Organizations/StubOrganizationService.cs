namespace HTMX.Test.Unit.Application.Web.Pages.Organizations;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Stands in for the organization service in page tests: it keeps each request the page sends and answers with an
/// empty page for it, so a test sees what the page asked for without a database.
/// </summary>
internal sealed class StubOrganizationService : IOrganizationService
{
    private readonly List<OrganizationCollectionRequest> _received = [];

    /// <summary>
    /// Gives the last request the page sent.
    /// </summary>
    /// <returns>The request, or <see langword="null"/> before the page sends one.</returns>
    public OrganizationCollectionRequest? LastRequest()
    {
        return this._received.LastOrDefault();
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
}
