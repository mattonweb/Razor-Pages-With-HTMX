namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What a list of users can be sorted by.
/// </summary>
public enum UserSortColumn
{
    /// <summary>
    /// The user's name as the application shows it, however it is capitalized.
    /// </summary>
    Name,

    /// <summary>
    /// The name the user signs in with, however it is capitalized.
    /// </summary>
    Username,

    /// <summary>
    /// The user's email address, however it is capitalized.
    /// </summary>
    Email,

    /// <summary>
    /// When the user was last changed.
    /// </summary>
    Updated,
}
