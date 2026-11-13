namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// One version of a contact, as their History tab shows it: what happened, who did it and when, and the contact's
/// values as that change left them.
/// </summary>
public sealed record ContactVersionResponse
{
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
    /// Gets when it happened, and who did it.
    /// </summary>
    required public RecordChange Changed { get; init; }

    /// <summary>
    /// Gets the contact's name as this version left it.
    /// </summary>
    required public DisplayName Name { get; init; }

    /// <summary>
    /// Gets the contact's job title as this version left it, if they had one.
    /// </summary>
    public JobTitle? JobTitle { get; init; }

    /// <summary>
    /// Gets the contact's email address as this version left it, if they had one.
    /// </summary>
    public EmailAddress? Email { get; init; }

    /// <summary>
    /// Gets the contact's telephone numbers as this version left them, which may be none.
    /// </summary>
    public PhoneNumbers PhoneNumbers { get; init; } = PhoneNumbers.None;

    /// <summary>
    /// Gets the notes about the contact as this version left them, if there were any.
    /// </summary>
    public Notes? Notes { get; init; }

    /// <summary>
    /// Gets the identifier of the organization the contact belonged to as this version left them, which a version
    /// keeps even once that organization is deleted.
    /// </summary>
    required public OrganizationId OrganizationId { get; init; }

    /// <summary>
    /// Gets that organization as it is now, or <see langword="null"/> when it no longer exists.
    /// </summary>
    public OrganizationReference? Organization { get; init; }

    /// <summary>
    /// Gets when the contact was archived, if this version left them archived.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }
}
