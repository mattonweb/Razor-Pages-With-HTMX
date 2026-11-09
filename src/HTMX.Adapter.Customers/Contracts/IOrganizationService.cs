namespace HTMX.Adapter.Customers.Contracts;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Reads and changes the customer database's organizations.
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// Reads one page of the organizations list.
    /// </summary>
    /// <param name="request">The filter, status, sort and page to read.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the page.</param>
    /// <returns>The page, and the counts a status filter and a pager show.</returns>
    Task<OrganizationCollectionResponse> ListAsync(
        OrganizationCollectionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one organization: every value it holds, how many contacts it has, and who created, last changed and
    /// archived it.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the organization.</param>
    /// <returns>The organization, or <see langword="null"/> when no organization has the identifier.</returns>
    Task<OrganizationSingleResponse?> FindAsync(OrganizationId id, CancellationToken cancellationToken = default);
}
