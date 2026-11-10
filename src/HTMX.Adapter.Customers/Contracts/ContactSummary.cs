namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// One contact as a list shows them: enough to recognize and reach them, not every value they hold.
/// </summary>
public sealed record ContactSummary
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
    /// Gets the person's telephone number, or <see langword="null"/> when it is not on record.
    /// </summary>
    public PhoneNumber? Phone { get; init; }

    /// <summary>
    /// Gets when the contact was archived, in UTC, or <see langword="null"/> while they are active.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }

    /// <summary>
    /// Gets the version the contact is at, which an action that cannot be undone, sent from the list, names.
    /// </summary>
    required public VersionNumber Version { get; init; }
}
