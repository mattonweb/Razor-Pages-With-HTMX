namespace HTMX.Adapter.Contacts.Entities;

using System;
using HTMX.Adapter.Contacts.Contracts;

/// <summary>
/// A person who uses the application, as the Users table holds them: their current values, who created them, who
/// changed them last and, once archived, who archived them.
/// </summary>
/// <remarks>
/// Every change to a user also writes a <see cref="UserVersion"/>, so the History tab can show what changed and roll it
/// back. A user is active while <see cref="ArchivedOn"/> is not set. Every date is in UTC.
/// </remarks>
internal sealed record User
{
    /// <summary>
    /// Gets the identifier, which the database assigns when the user is first saved.
    /// </summary>
    public UserId Id { get; init; }

    /// <summary>
    /// Gets the name the user is known by, unique however it is capitalized.
    /// </summary>
    required public Username Username { get; init; }

    /// <summary>
    /// Gets the email address, unique however it is capitalized.
    /// </summary>
    required public EmailAddress Email { get; init; }

    /// <summary>
    /// Gets the person's name as the application shows it.
    /// </summary>
    required public DisplayName DisplayName { get; init; }

    /// <summary>
    /// Gets when the user was created.
    /// </summary>
    required public DateTime CreatedOn { get; init; }

    /// <summary>
    /// Gets who created the user.
    /// </summary>
    required public UserId CreatedBy { get; init; }

    /// <summary>
    /// Gets when the user was last changed, which is when they were created until their first change.
    /// </summary>
    required public DateTime LastModifiedOn { get; init; }

    /// <summary>
    /// Gets who changed the user last.
    /// </summary>
    required public UserId LastModifiedBy { get; init; }

    /// <summary>
    /// Gets when the user was archived, or <see langword="null"/> while they are active.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }

    /// <summary>
    /// Gets who archived the user, or <see langword="null"/> while they are active.
    /// </summary>
    public UserId? ArchivedBy { get; init; }

    /// <summary>
    /// Gets the number of the user's current version. A save that started from an older number is refused.
    /// </summary>
    required public VersionNumber Version { get; init; }
}
