namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// A user as another record names them, such as the user who created or changed it.
/// </summary>
public sealed record UserReference
{
    /// <summary>
    /// Gets the user's identifier.
    /// </summary>
    required public UserId Id { get; init; }

    /// <summary>
    /// Gets the name the user goes by.
    /// </summary>
    required public DisplayName Name { get; init; }

    /// <summary>
    /// Gets when the user was archived, in UTC, or <see langword="null"/> while they are active.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }
}
