namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A request to save an earlier version's values again, as the organization's next version.
/// </summary>
public sealed record OrganizationRollBackRequest
{
    /// <summary>
    /// Gets the identifier of the organization to roll back.
    /// </summary>
    required public OrganizationId Id { get; init; }

    /// <summary>
    /// Gets the version the organization was at when the roll-back was asked for. It is rolled back only while it is
    /// still at that version.
    /// </summary>
    required public VersionNumber Version { get; init; }

    /// <summary>
    /// Gets the earlier version whose values to save again.
    /// </summary>
    required public VersionNumber To { get; init; }

    /// <summary>
    /// Gets who is rolling it back: the signed-in user.
    /// </summary>
    required public UserId ChangedBy { get; init; }
}
