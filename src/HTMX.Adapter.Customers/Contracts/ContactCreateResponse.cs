namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What became of a request to create a contact and, when they were created, the identifier they were given.
/// </summary>
public sealed record ContactCreateResponse
{
    /// <summary>
    /// Gets whether the contact was created and, if not, why.
    /// </summary>
    required public CreateOutcome Outcome { get; init; }

    /// <summary>
    /// Gets the new contact's identifier, or <see langword="null"/> when nothing was created.
    /// </summary>
    public ContactId? Id { get; init; }
}
