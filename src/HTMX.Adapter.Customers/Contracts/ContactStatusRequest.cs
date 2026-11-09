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
    /// Gets the organization whose contacts the request was made among. A contact who has moved to another
    /// organization since is not changed.
    /// </summary>
    required public OrganizationId Organization { get; init; }

    /// <summary>
    /// Gets who is archiving or restoring the contact: the signed-in user.
    /// </summary>
    required public UserId ChangedBy { get; init; }
}
