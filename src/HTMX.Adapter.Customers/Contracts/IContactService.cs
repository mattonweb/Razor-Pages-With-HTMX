namespace HTMX.Adapter.Customers.Contracts;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Reads and changes the customer database's contacts.
/// </summary>
public interface IContactService
{
    /// <summary>
    /// Reads one page of an organization's contacts, in name order, archived ones among the rest.
    /// </summary>
    /// <param name="request">The organization and the page to read.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the page.</param>
    /// <returns>The page, and the count a pager shows; an empty page when no organization has the identifier.</returns>
    Task<ContactCollectionResponse> ListAsync(
        ContactCollectionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one contact: every value they hold, the organization they belong to, and who created, last changed and
    /// archived them.
    /// </summary>
    /// <param name="id">The contact's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the contact.</param>
    /// <returns>The contact, or <see langword="null"/> when no contact has the identifier.</returns>
    Task<ContactSingleResponse?> FindAsync(ContactId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads every version of one contact, newest first, each with who made it and the organization it names.
    /// </summary>
    /// <param name="id">The contact's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the versions.</param>
    /// <returns>The versions, newest first; none when no contact has the identifier.</returns>
    Task<ContactVersionResponse[]> ListVersionsAsync(ContactId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a contact, with their first version, unless the organization they would belong to is archived or does
    /// not exist.
    /// </summary>
    /// <param name="request">The new contact's values, their organization, and who is creating them.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Whether the contact was created and, if they were, their identifier.</returns>
    Task<ContactCreateResponse> CreateAsync(
        ContactCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a change to one contact's values as their next version, unless the contact or their organization is
    /// archived, the contact has moved on since the form was filled, or the organization the change names, when it is
    /// another, takes no one in.
    /// </summary>
    /// <param name="request">The values to save, the version they started from, and who is saving them.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Whether the change was saved and, if not, why.</returns>
    Task<UpdateOutcome> UpdateAsync(ContactUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an earlier version's values again, as the contact's next version, under the same rules as a change: the
    /// contact and their organization must be active, the contact still at the version the request names, holding
    /// some value other than the earlier version's, and the organization the earlier version names, when it is
    /// another, must still take people in.
    /// </summary>
    /// <param name="request">Which contact, the version they are at, the version to roll back to, and who asks.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Whether the values were saved and, if not, why; NotFound also when the earlier version does not
    /// exist.</returns>
    Task<UpdateOutcome> RollBackAsync(ContactRollBackRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives an active contact as their next version. Their values and their place in the organization are kept.
    /// </summary>
    /// <param name="request">Which contact, the organization they belong to, and who is archiving them.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Saved when they were archived; Unchanged when they already were; Archived when their organization is,
    /// which closes its contacts to changes; NotFound when there is no such contact, or the organization the request
    /// names has none; Overtaken when another save got there first.</returns>
    Task<UpdateOutcome> ArchiveAsync(ContactStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores an archived contact as their next version, making them active again.
    /// </summary>
    /// <param name="request">Which contact, the organization they belong to, and who is restoring them.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Saved when they were restored; Unchanged when they were already active; otherwise Archived, NotFound
    /// or Overtaken, as for <see cref="ArchiveAsync"/>.</returns>
    Task<UpdateOutcome> RestoreAsync(ContactStatusRequest request, CancellationToken cancellationToken = default);
}
