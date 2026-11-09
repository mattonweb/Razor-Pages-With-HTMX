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
    /// Archives an active contact as their next version. Their values and their place in the organization are kept.
    /// </summary>
    /// <param name="request">Which contact, the organization they belong to, and who is archiving them.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the answer.</param>
    /// <returns>Saved when they were archived; Unchanged when they already were; Archived when their organization is,
    /// which closes its contacts to changes; NotFound when the organization has no such contact; Overtaken when
    /// another save got there first.</returns>
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
