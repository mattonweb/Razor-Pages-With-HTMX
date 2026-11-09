namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A request to save an earlier version of a contact's values again, as their next version.
/// </summary>
public sealed record ContactRollBackRequest
{
    /// <summary>
    /// Gets the identifier of the contact to roll back.
    /// </summary>
    required public ContactId Id { get; init; }

    /// <summary>
    /// Gets the version the contact was at when the roll-back was asked for. A roll-back from any other version is
    /// refused, as a change from an out-of-date form is.
    /// </summary>
    required public VersionNumber Version { get; init; }

    /// <summary>
    /// Gets the earlier version whose values to save again.
    /// </summary>
    required public VersionNumber To { get; init; }

    /// <summary>
    /// Gets who is rolling the contact back: the signed-in user.
    /// </summary>
    required public UserId ChangedBy { get; init; }
}
