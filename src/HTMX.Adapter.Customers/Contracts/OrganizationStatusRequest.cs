namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A request to archive an organization or to restore it: which one, and who is asking.
/// </summary>
public sealed record OrganizationStatusRequest
{
    /// <summary>
    /// Gets the identifier of the organization to archive or restore.
    /// </summary>
    required public OrganizationId Id { get; init; }

    /// <summary>
    /// Gets who is archiving or restoring it: the signed-in user.
    /// </summary>
    required public UserId ChangedBy { get; init; }
}
