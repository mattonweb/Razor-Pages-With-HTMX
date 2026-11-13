namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// One contact with every value they hold, the organization they belong to, and who created, last changed and
/// archived them.
/// </summary>
public sealed record ContactSingleResponse
{
    /// <summary>
    /// Gets the contact's identifier.
    /// </summary>
    required public ContactId Id { get; init; }

    /// <summary>
    /// Gets the person's name.
    /// </summary>
    required public DisplayName Name { get; init; }

    /// <summary>
    /// Gets the person's job title, or <see langword="null"/> when it is not on record.
    /// </summary>
    public JobTitle? JobTitle { get; init; }

    /// <summary>
    /// Gets the person's email address, or <see langword="null"/> when it is not on record.
    /// </summary>
    public EmailAddress? Email { get; init; }

    /// <summary>
    /// Gets the person's telephone numbers, in the order they were written; none when none is on record.
    /// </summary>
    public PhoneNumbers PhoneNumbers { get; init; } = PhoneNumbers.None;

    /// <summary>
    /// Gets free text about the person, or <see langword="null"/> when there is none.
    /// </summary>
    public Notes? Notes { get; init; }

    /// <summary>
    /// Gets the organization the person belongs to.
    /// </summary>
    required public OrganizationReference Organization { get; init; }

    /// <summary>
    /// Gets the number of the contact's current version.
    /// </summary>
    required public VersionNumber Version { get; init; }

    /// <summary>
    /// Gets when the contact was created, and by whom.
    /// </summary>
    required public RecordChange Created { get; init; }

    /// <summary>
    /// Gets when the contact was last changed, and by whom.
    /// </summary>
    required public RecordChange LastModified { get; init; }

    /// <summary>
    /// Gets when the contact was archived, and by whom, or <see langword="null"/> while they are active.
    /// </summary>
    public RecordChange? Archived { get; init; }
}
