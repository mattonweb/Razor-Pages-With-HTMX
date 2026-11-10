namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A new contact, as their create form sends them: their values, the organization they belong to, and who is creating
/// them.
/// </summary>
public sealed record ContactCreateRequest
{
    /// <summary>
    /// Gets the person's name.
    /// </summary>
    required public DisplayName Name { get; init; }

    /// <summary>
    /// Gets the person's job title, or <see langword="null"/> for none.
    /// </summary>
    public JobTitle? JobTitle { get; init; }

    /// <summary>
    /// Gets the person's email address, or <see langword="null"/> for none.
    /// </summary>
    public EmailAddress? Email { get; init; }

    /// <summary>
    /// Gets the person's telephone number, or <see langword="null"/> for none.
    /// </summary>
    public PhoneNumber? Phone { get; init; }

    /// <summary>
    /// Gets free text about the person, or <see langword="null"/> for none.
    /// </summary>
    public Notes? Notes { get; init; }

    /// <summary>
    /// Gets the organization the person belongs to, which must be active to take them in.
    /// </summary>
    required public OrganizationId Organization { get; init; }

    /// <summary>
    /// Gets who is creating the contact: the signed-in user.
    /// </summary>
    required public UserId CreatedBy { get; init; }
}
