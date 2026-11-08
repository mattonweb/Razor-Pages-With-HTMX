namespace HTMX.Adapter.Customers.Entities;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A person at one of the customer organizations, as the Contacts table holds them: their current values, the
/// organization they belong to, who created them, who changed them last and, once archived, who archived them.
/// </summary>
/// <remarks>
/// Every contact belongs to exactly one organization, and can move to another. Every change to a contact also writes a
/// <see cref="ContactVersion"/>, so the History tab can show what changed and roll it back. A contact is active while
/// <see cref="ArchivedOn"/> is not set. Every date is in UTC.
/// </remarks>
internal sealed record Contact
{
    /// <summary>
    /// Gets the identifier, which the database assigns when the contact is first saved.
    /// </summary>
    public ContactId Id { get; init; }

    /// <summary>
    /// Gets the person's name.
    /// </summary>
    required public DisplayName Name { get; init; }

    /// <summary>
    /// Gets the person's email address, or <see langword="null"/> when it is not on record.
    /// </summary>
    public EmailAddress? Email { get; init; }

    /// <summary>
    /// Gets the person's telephone number, or <see langword="null"/> when it is not on record.
    /// </summary>
    public PhoneNumber? Phone { get; init; }

    /// <summary>
    /// Gets the person's job title, or <see langword="null"/> when it is not on record.
    /// </summary>
    public JobTitle? JobTitle { get; init; }

    /// <summary>
    /// Gets the organization the person belongs to.
    /// </summary>
    required public OrganizationId OrganizationId { get; init; }

    /// <summary>
    /// Gets free text about the person, or <see langword="null"/> when there is none.
    /// </summary>
    public Notes? Notes { get; init; }

    /// <summary>
    /// Gets when the contact was created.
    /// </summary>
    required public DateTime CreatedOn { get; init; }

    /// <summary>
    /// Gets who created the contact.
    /// </summary>
    required public UserId CreatedBy { get; init; }

    /// <summary>
    /// Gets when the contact was last changed, which is when they were created until their first change.
    /// </summary>
    required public DateTime LastModifiedOn { get; init; }

    /// <summary>
    /// Gets who changed the contact last.
    /// </summary>
    required public UserId LastModifiedBy { get; init; }

    /// <summary>
    /// Gets when the contact was archived, or <see langword="null"/> while they are active.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }

    /// <summary>
    /// Gets who archived the contact, or <see langword="null"/> while they are active.
    /// </summary>
    public UserId? ArchivedBy { get; init; }

    /// <summary>
    /// Gets the number of the contact's current version. A save that started from an older number is refused.
    /// </summary>
    required public VersionNumber Version { get; init; }
}
