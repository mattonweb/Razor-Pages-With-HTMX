namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A new organization, as its create form sends it: its values and who is creating it.
/// </summary>
public sealed record OrganizationCreateRequest
{
    /// <summary>
    /// Gets the new organization's name.
    /// </summary>
    required public OrganizationName Name { get; init; }

    /// <summary>
    /// Gets the industry the new organization works in.
    /// </summary>
    required public Industry Industry { get; init; }

    /// <summary>
    /// Gets the new organization's website, or <see langword="null"/> when it has none.
    /// </summary>
    public WebAddress? Website { get; init; }

    /// <summary>
    /// Gets who is creating the organization: the signed-in user.
    /// </summary>
    required public UserId CreatedBy { get; init; }
}
