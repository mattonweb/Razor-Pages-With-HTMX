namespace HTMX.Adapter.Customers.Entities;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// One version of a user: what happened, who did it and when, and a copy of the user's values as that change left
/// them.
/// </summary>
/// <remarks>
/// Versions are only ever added. The History tab compares each version with the one before it to show what changed,
/// and a roll-back saves an older version's values as a new version, so history is never rewritten.
/// </remarks>
internal sealed record UserVersion
{
    /// <summary>
    /// Gets the user this is a version of.
    /// </summary>
    required public UserId UserId { get; init; }

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
    /// Gets the user's name as this version left it.
    /// </summary>
    required public Username Username { get; init; }

    /// <summary>
    /// Gets the user's email address as this version left it.
    /// </summary>
    required public EmailAddress Email { get; init; }

    /// <summary>
    /// Gets the user's display name as this version left it.
    /// </summary>
    required public DisplayName DisplayName { get; init; }

    /// <summary>
    /// Gets when the user was archived, if this version left them archived.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }

    /// <summary>
    /// Gets who archived the user, if this version left them archived.
    /// </summary>
    public UserId? ArchivedBy { get; init; }
}
