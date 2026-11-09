namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A change to one organization's values, as its edit form sends it: the values to save, the version the form was
/// filled from, and who is saving.
/// </summary>
public sealed record OrganizationUpdateRequest
{
    /// <summary>
    /// Gets the identifier of the organization to change.
    /// </summary>
    required public OrganizationId Id { get; init; }

    /// <summary>
    /// Gets the version the form was filled from. The change is saved only while the organization is still at it.
    /// </summary>
    required public VersionNumber Version { get; init; }

    /// <summary>
    /// Gets the name to save.
    /// </summary>
    required public OrganizationName Name { get; init; }

    /// <summary>
    /// Gets the industry to save.
    /// </summary>
    required public Industry Industry { get; init; }

    /// <summary>
    /// Gets the website to save, or <see langword="null"/> to have none on record.
    /// </summary>
    public WebAddress? Website { get; init; }

    /// <summary>
    /// Gets who is saving the change: the signed-in user.
    /// </summary>
    required public UserId ChangedBy { get; init; }
}
