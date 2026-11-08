namespace HTMX.Adapter.Customers.Entities;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// One version of a contact: what happened, who did it and when, and a copy of the contact's values as that change
/// left them.
/// </summary>
/// <remarks>
/// Versions are only ever added. The History tab compares each version with the one before it to show what changed,
/// and a roll-back saves an older version's values as a new version, so history is never rewritten. A version keeps the
/// organization the contact belonged to then, even if that organization has since been deleted.
/// </remarks>
internal sealed record ContactVersion
{
    /// <summary>
    /// Gets the contact this is a version of.
    /// </summary>
    required public ContactId ContactId { get; init; }

    /// <summary>
    /// Gets the version's number, counting from 1.
    /// </summary>
    required public VersionNumber Version { get; init; }

    /// <summary>
    /// Gets what happened.
    /// </summary>
    required public ChangeKind Change { get; init; }

    /// <summary>
    /// Gets the version whose values a roll-back saved again, or <see langword="null"/> for any other change.
    /// </summary>
    public VersionNumber? RolledBackTo { get; init; }

    /// <summary>
    /// Gets when it happened.
    /// </summary>
    required public DateTime ChangedOn { get; init; }

    /// <summary>
    /// Gets who did it.
    /// </summary>
    required public UserId ChangedBy { get; init; }

    /// <summary>
    /// Gets the contact's name as this version left it.
    /// </summary>
    required public DisplayName Name { get; init; }

    /// <summary>
    /// Gets the contact's email address as this version left it, if they had one.
    /// </summary>
    public EmailAddress? Email { get; init; }

    /// <summary>
    /// Gets the contact's telephone number as this version left it, if they had one.
    /// </summary>
    public PhoneNumber? Phone { get; init; }

    /// <summary>
    /// Gets the contact's job title as this version left it, if they had one.
    /// </summary>
    public JobTitle? JobTitle { get; init; }

    /// <summary>
    /// Gets the organization the contact belonged to as this version left them.
    /// </summary>
    required public OrganizationId OrganizationId { get; init; }

    /// <summary>
    /// Gets the notes about the contact as this version left them, if there were any.
    /// </summary>
    public Notes? Notes { get; init; }

    /// <summary>
    /// Gets when the contact was archived, if this version left them archived.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }

    /// <summary>
    /// Gets who archived the contact, if this version left them archived.
    /// </summary>
    public UserId? ArchivedBy { get; init; }
}
