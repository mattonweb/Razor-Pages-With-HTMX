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
    /// Reads every active organization, in name order: the ones a contact can be put in, as a select offers them.
    /// </summary>
    /// <param name="cancellationToken">Signals that the caller no longer needs the organizations.</param>
    /// <returns>The organizations.</returns>
    Task<OrganizationReference[]> ListChoicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one organization: every value it holds, how many contacts it has, and who created, last changed and
    /// archived it.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the organization.</param>
    /// <returns>The organization, or <see langword="null"/> when no organization has the identifier.</returns>
    Task<OrganizationSingleResponse?> FindAsync(OrganizationId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads every version of one organization, newest first, each with who made it.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the versions.</param>
    /// <returns>The versions, newest first; none when no organization has the identifier.</returns>
    Task<OrganizationVersionResponse[]> ListVersionsAsync(
        OrganizationId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an organization, with its first version, unless another organization already has its name.
    /// </summary>
    /// <param name="request">The new organization's values, and who is creating it.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Whether the organization was created and, if it was, its identifier.</returns>
    Task<OrganizationCreateResponse> CreateAsync(
        OrganizationCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a change to one organization's values as its next version, unless the organization has moved on since the
    /// form was filled, is archived, or would take another organization's name.
    /// </summary>
    /// <param name="request">The values to save, the version they started from, and who is saving them.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Whether the change was saved and, if not, why.</returns>
    Task<UpdateOutcome> UpdateAsync(OrganizationUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an earlier version's values again, as the organization's next version, under the same rules as a change:
    /// the organization must be active, still at the version the request names, hold some value other than the earlier
    /// version's, and the earlier name must not have passed to another organization since.
    /// </summary>
    /// <param name="request">Which organization, the version it is at, the version to roll back to, and who asks.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Whether the values were saved and, if not, why; NotFound also when the earlier version does not
    /// exist.</returns>
    Task<UpdateOutcome> RollBackAsync(
        OrganizationRollBackRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives an active organization as its next version: it leaves the Active list and is closed to edits until it
    /// is restored. Its values and its contacts are kept.
    /// </summary>
    /// <param name="request">Which organization, and who is archiving it.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Saved when it was archived; Unchanged when it already was; NotFound or Overtaken otherwise.</returns>
    Task<UpdateOutcome> ArchiveAsync(OrganizationStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores an archived organization as its next version, making it active and open to edits again.
    /// </summary>
    /// <param name="request">Which organization, and who is restoring it.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Saved when it was restored; Unchanged when it was already active; NotFound or Overtaken
    /// otherwise.</returns>
    Task<UpdateOutcome> RestoreAsync(OrganizationStatusRequest request, CancellationToken cancellationToken = default);
}
