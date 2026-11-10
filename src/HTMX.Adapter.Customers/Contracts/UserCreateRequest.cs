namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A new user, as their create form sends them: their values and who is creating them.
/// </summary>
public sealed record UserCreateRequest
{
    /// <summary>
    /// Gets the new user's name, as the application shows it.
    /// </summary>
    required public DisplayName DisplayName { get; init; }

    /// <summary>
    /// Gets the new user's username, which no other user may have, however it is capitalized.
    /// </summary>
    required public Username Username { get; init; }

    /// <summary>
    /// Gets the new user's email address, which no other user may have, however it is capitalized.
    /// </summary>
    required public EmailAddress Email { get; init; }

    /// <summary>
    /// Gets who is creating the user: the signed-in user.
    /// </summary>
    required public UserId CreatedBy { get; init; }
}
