namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// One user as a list shows them: enough to recognize them, not every value they hold.
/// </summary>
public sealed record UserSummary
{
    /// <summary>
    /// Gets the user's identifier.
    /// </summary>
    required public UserId Id { get; init; }

    /// <summary>
    /// Gets the person's name as the application shows it.
    /// </summary>
    required public DisplayName DisplayName { get; init; }

    /// <summary>
    /// Gets the name the user signs in with.
    /// </summary>
    required public Username Username { get; init; }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    required public EmailAddress Email { get; init; }

    /// <summary>
    /// Gets when the user was last changed, in UTC.
    /// </summary>
    required public DateTime LastModifiedOn { get; init; }

    /// <summary>
    /// Gets when the user was archived, in UTC, or <see langword="null"/> while they are active.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }
}
