namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// One version of a user, as their History tab shows it: what happened, who did it and when, and the user's values as
/// that change left them.
/// </summary>
public sealed record UserVersionResponse
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
    /// Gets the user's name, as the application shows it, as this version left it.
    /// </summary>
    required public DisplayName DisplayName { get; init; }

    /// <summary>
    /// Gets the name the user signed in with as this version left it.
    /// </summary>
    required public Username Username { get; init; }

    /// <summary>
    /// Gets the user's email address as this version left it.
    /// </summary>
    required public EmailAddress Email { get; init; }

    /// <summary>
    /// Gets when the user was archived, if this version left them archived.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }
}
