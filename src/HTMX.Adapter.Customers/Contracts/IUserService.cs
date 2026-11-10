namespace HTMX.Adapter.Customers.Contracts;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Reads the customer database's users: the people who use the application and manage its records.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Reads one page of the users list: filtered, narrowed by status and sorted, with the counts a status filter and
    /// a pager show.
    /// </summary>
    /// <param name="request">The filter, status, sort and page to read.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the page.</param>
    /// <returns>The page; the first page when the request's cursor names a user who no longer exists, or leaves too
    /// little to show.</returns>
    Task<UserCollectionResponse> ListAsync(
        UserCollectionRequest request,
        CancellationToken cancellationToken = default);
}
