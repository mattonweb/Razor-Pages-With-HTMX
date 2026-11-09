namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What became of a request to create an organization and, when it was created, the identifier it was given.
/// </summary>
public sealed record OrganizationCreateResponse
{
    /// <summary>
    /// Gets whether the organization was created and, if not, why.
    /// </summary>
    required public CreateOutcome Outcome { get; init; }

    /// <summary>
    /// Gets the new organization's identifier, or <see langword="null"/> when nothing was created.
    /// </summary>
    public OrganizationId? Id { get; init; }
}
