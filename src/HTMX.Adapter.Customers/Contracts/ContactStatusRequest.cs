namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A request to archive a contact or to restore them: which one, the organization they belong to, and who is asking.
/// </summary>
public sealed record ContactStatusRequest
{
    /// <summary>
    /// Gets the identifier of the contact to archive or restore.
    /// </summary>
    required public ContactId Id { get; init; }

    /// <summary>
    /// Gets the organization whose contacts the request was made among, or <see langword="null"/> when it was made on
    /// the contact's own page. A contact who has moved to another organization since the one named is not changed.
    /// </summary>
    public OrganizationId? Organization { get; init; }

    /// <summary>
    /// Gets who is archiving or restoring the contact: the signed-in user.
    /// </summary>
    required public UserId ChangedBy { get; init; }
}
