namespace HTMX.Adapter.Customers.Contracts;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Reads and changes the customer database's users: the people who use the application and manage its records.
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

    /// <summary>
    /// Reads one user: every value they hold, and who created, last changed and archived them.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the user.</param>
    /// <returns>The user, or <see langword="null"/> when no user has the identifier.</returns>
    Task<UserSingleResponse?> FindAsync(UserId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads every version of one user, newest first, each with who made it.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the versions.</param>
    /// <returns>The versions, newest first; none when no user has the identifier.</returns>
    Task<UserVersionResponse[]> ListVersionsAsync(UserId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an earlier version's values again, as the user's next version: the user must be active, still at the
    /// version the request names, and holding some value other than the earlier version's, and no other user may
    /// have the earlier version's username or email address now.
    /// </summary>
    /// <param name="request">Which user, the version they are at, the version to roll back to, and who asks.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Whether the values were saved and, if not, why: NameTaken for the username, EmailTaken for the email
    /// address; NotFound also when the earlier version does not exist.</returns>
    Task<UpdateOutcome> RollBackAsync(UserRollBackRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives an active user as their next version, unless they are the one asking. Their values are kept, and so
    /// is every record that names them.
    /// </summary>
    /// <param name="request">Which user, and who is archiving them.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Saved when they were archived; SelfArchive when they asked to archive themselves; Unchanged when they
    /// already were; NotFound when there is no such user; Overtaken when another save got there first.</returns>
    Task<UpdateOutcome> ArchiveAsync(UserStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores an archived user as their next version, making them active again.
    /// </summary>
    /// <param name="request">Which user, and who is restoring them.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Saved when they were restored; Unchanged when they were already active; otherwise NotFound or
    /// Overtaken, as for <see cref="ArchiveAsync"/>.</returns>
    Task<UpdateOutcome> RestoreAsync(UserStatusRequest request, CancellationToken cancellationToken = default);
}
