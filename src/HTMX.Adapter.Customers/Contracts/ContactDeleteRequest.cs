namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A request to delete a contact permanently, with their history, as their page's confirm dialog sends it.
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
    /// Gets the version the page showed the contact at. They are deleted only while they are still at it, so a confirm
    /// dialog never deletes a contact who has changed since they were shown.
    /// </summary>
    required public VersionNumber Version { get; init; }
}
