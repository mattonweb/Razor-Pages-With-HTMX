namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A request to archive a user or to restore them: which one, and who is asking.
/// </summary>
public sealed record UserStatusRequest
{
    /// <summary>
    /// Gets the identifier of the user to archive or restore.
    /// </summary>
    required public UserId Id { get; init; }

    /// <summary>
    /// Gets who is archiving or restoring them: the signed-in user, who may not archive themselves.
    /// </summary>
    required public UserId ChangedBy { get; init; }
}
