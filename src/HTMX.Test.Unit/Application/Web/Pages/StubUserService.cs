namespace HTMX.Test.Unit.Application.Web.Pages;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Stands in for the user service in page tests: it keeps each request the page sends and answers with an empty page,
/// so a test sees what the page asked for without a database.
/// </summary>
internal sealed class StubUserService : IUserService
{
    private readonly List<UserCollectionRequest> _received = [];

    /// <summary>
    /// Gives the last request the page sent.
    /// </summary>
    /// <returns>The request, or <see langword="null"/> before the page sends one.</returns>
    public UserCollectionRequest? LastRequest()
    {
        return this._received.LastOrDefault();
    }

    /// <inheritdoc/>
    public Task<UserCollectionResponse> ListAsync(
        UserCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        this._received.Add(request);
        UserCollectionResponse response = new UserCollectionResponse
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
