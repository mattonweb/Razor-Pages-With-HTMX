namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A request to delete an organization permanently, with its history, as its page's confirm dialog sends it.
/// </summary>
/// <remarks>
/// It names no one as who asked: nothing is left to record it on. Contacts' histories that name the organization keep
/// its identifier, and say it no longer exists.
/// </remarks>
public sealed record OrganizationDeleteRequest
{
    /// <summary>
    /// Gets the identifier of the organization to delete.
    /// </summary>
    required public OrganizationId Id { get; init; }

    /// <summary>
    /// Gets the version the page showed the organization at. It is deleted only while it is still at it, so a confirm
    /// dialog never deletes an organization that has changed since it was shown.
    /// </summary>
    required public VersionNumber Version { get; init; }
}
