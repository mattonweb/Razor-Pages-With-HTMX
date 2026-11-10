namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A change to one user's values, as their edit form sends it: the values to save, the version the form was filled
/// from, and who is saving.
/// </summary>
public sealed record UserUpdateRequest
{
    /// <summary>
    /// Gets the identifier of the user to change.
    /// </summary>
    required public UserId Id { get; init; }

    /// <summary>
    /// Gets the version the form was filled from. The change is saved only while the user is still at it.
    /// </summary>
    required public VersionNumber Version { get; init; }

    /// <summary>
    /// Gets the name to save, as the application shows it.
    /// </summary>
    required public DisplayName DisplayName { get; init; }

    /// <summary>
    /// Gets the username to save, which no other user may have, however it is capitalized.
    /// </summary>
    required public Username Username { get; init; }

    /// <summary>
    /// Gets the email address to save, which no other user may have, however it is capitalized.
    /// </summary>
    required public EmailAddress Email { get; init; }

    /// <summary>
    /// Gets who is saving the change: the signed-in user.
    /// </summary>
    required public UserId ChangedBy { get; init; }
}
