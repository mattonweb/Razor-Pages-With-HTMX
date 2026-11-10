namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A request to delete a user permanently, with their history, as their page's confirm dialog sends it.
/// </summary>
/// <remarks>
/// It names no one as who asked: nothing is left to record it on. The user's own history goes with them.
/// </remarks>
public sealed record UserDeleteRequest
{
    /// <summary>
    /// Gets the identifier of the user to delete.
    /// </summary>
    required public UserId Id { get; init; }

    /// <summary>
    /// Gets the version the page showed the user at. They are deleted only while they are still at it, so a confirm
    /// dialog never deletes a user who has changed since it was shown.
    /// </summary>
    required public VersionNumber Version { get; init; }
}
