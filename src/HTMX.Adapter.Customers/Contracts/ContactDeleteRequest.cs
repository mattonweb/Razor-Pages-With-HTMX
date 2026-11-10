namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A request to delete a contact permanently, with their history, as a confirm dialog sends it: on their own page, or
/// from their row on their organization's Contacts tab.
/// </summary>
/// <remarks>
/// It names no one as who asked: nothing is left to record it on. Nothing else names a contact, so nothing is left
/// naming them.
/// </remarks>
public sealed record ContactDeleteRequest
{
    /// <summary>
    /// Gets the identifier of the contact to delete.
    /// </summary>
    required public ContactId Id { get; init; }

    /// <summary>
    /// Gets the organization whose contacts the request was made among, or <see langword="null"/> when it was made on
    /// the contact's own page. A contact who has moved to another organization since the one named is not deleted.
    /// </summary>
    public OrganizationId? Organization { get; init; }

    /// <summary>
    /// Gets the version the page showed the contact at. They are deleted only while they are still at it, so a confirm
    /// dialog never deletes a contact who has changed since they were shown.
    /// </summary>
    required public VersionNumber Version { get; init; }
}
