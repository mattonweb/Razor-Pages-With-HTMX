namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Identifies one organization.
/// </summary>
/// <remarks>
/// Unlike the other wrapped values, an identifier is not checked when it is made. Entity Framework gives a new
/// organization a negative placeholder identifier until the database assigns the real one, and it builds that
/// placeholder through this constructor.
/// </remarks>
public readonly record struct OrganizationId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationId"/> class.
    /// </summary>
    /// <param name="value">The identifier the database gave the organization.</param>
    public OrganizationId(long value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public long Value { get; }
}
