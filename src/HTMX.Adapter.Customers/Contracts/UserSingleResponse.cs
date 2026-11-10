namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// One user with every value they hold, and who created, last changed and archived them.
/// </summary>
public sealed record UserSingleResponse
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
    /// Gets the number of the user's current version.
    /// </summary>
    required public VersionNumber Version { get; init; }

    /// <summary>
    /// Gets when the user was created, and by whom.
    /// </summary>
    required public RecordChange Created { get; init; }

    /// <summary>
    /// Gets when the user was last changed, and by whom.
    /// </summary>
    required public RecordChange LastModified { get; init; }

    /// <summary>
    /// Gets when the user was archived, and by whom, or <see langword="null"/> while they are active.
    /// </summary>
    public RecordChange? Archived { get; init; }
}
