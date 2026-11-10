namespace HTMX.Adapter.Customers.DTOs;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The user a cursor names, as the list sorts them: their value in every sortable column, and their identifier, which
/// settles a tie. The next page starts just past these values, whichever column the list is sorted by.
/// </summary>
internal sealed record UserSortKey
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
}
