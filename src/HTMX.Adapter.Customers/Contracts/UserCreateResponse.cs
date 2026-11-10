namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What became of a request to create a user and, when they were created, the identifier they were given.
/// </summary>
public sealed record UserCreateResponse
{
    /// <summary>
    /// Gets whether the user was created and, if not, why.
    /// </summary>
    required public CreateOutcome Outcome { get; init; }

    /// <summary>
    /// Gets the new user's identifier, or <see langword="null"/> when nothing was created.
    /// </summary>
    public UserId? Id { get; init; }
}
