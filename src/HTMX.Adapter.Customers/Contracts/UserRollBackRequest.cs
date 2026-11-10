namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A request to save an earlier version of a user's values again, as their next version.
/// </summary>
public sealed record UserRollBackRequest
{
    /// <summary>
    /// Gets the identifier of the user to roll back.
    /// </summary>
    required public UserId Id { get; init; }

    /// <summary>
    /// Gets the version the user was at when the roll-back was asked for. They are rolled back only while they are
    /// still at that version.
    /// </summary>
    required public VersionNumber Version { get; init; }

    /// <summary>
    /// Gets the earlier version whose values to save again.
    /// </summary>
    required public VersionNumber To { get; init; }

    /// <summary>
    /// Gets who is rolling the user back: the signed-in user.
    /// </summary>
    required public UserId ChangedBy { get; init; }
}
