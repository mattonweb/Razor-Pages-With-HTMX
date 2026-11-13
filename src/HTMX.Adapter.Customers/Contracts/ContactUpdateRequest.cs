namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A change to one contact's values, the organization they belong to among them, as the contact's next version.
/// </summary>
public sealed record ContactUpdateRequest
{
    /// <summary>
    /// Gets the identifier of the contact to change.
    /// </summary>
    required public ContactId Id { get; init; }

    /// <summary>
    /// Gets the version the changes started from. A change from any other version is refused, so that one save never
    /// overwrites another without knowing it.
    /// </summary>
    required public VersionNumber Version { get; init; }

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
    /// Gets the person's telephone numbers, in order, or <see cref="PhoneNumbers.None"/> for none.
    /// </summary>
    public PhoneNumbers PhoneNumbers { get; init; } = PhoneNumbers.None;

    /// <summary>
    /// Gets free text about the person, or <see langword="null"/> for none.
    /// </summary>
    public Notes? Notes { get; init; }

    /// <summary>
    /// Gets the organization the person belongs to, which must be active to take them in.
    /// </summary>
    required public OrganizationId Organization { get; init; }

    /// <summary>
    /// Gets who is saving the changes: the signed-in user.
    /// </summary>
    required public UserId ChangedBy { get; init; }
}
