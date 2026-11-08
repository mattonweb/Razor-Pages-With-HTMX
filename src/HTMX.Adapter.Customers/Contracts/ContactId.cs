namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Identifies one contact.
/// </summary>
/// <remarks>
/// Unlike the other wrapped values, an identifier is not checked when it is made. Entity Framework gives a new
/// contact a negative placeholder identifier until the database assigns the real one, and it builds that
/// placeholder through this constructor.
/// </remarks>
public readonly record struct ContactId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContactId"/> class.
    /// </summary>
    /// <param name="value">The identifier the database gave the contact.</param>
    public ContactId(long value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public long Value { get; }
}
