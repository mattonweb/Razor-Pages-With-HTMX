namespace HTMX.Adapter.Contacts.Contracts;

/// <summary>
/// Identifies one user.
/// </summary>
/// <remarks>
/// Unlike the other wrapped values, an identifier is not checked when it is made. Entity Framework gives a new user a
/// negative placeholder identifier until the database assigns the real one, and it builds that placeholder through this
/// constructor.
/// </remarks>
public readonly record struct UserId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserId"/> class.
    /// </summary>
    /// <param name="value">The identifier the database gave the user.</param>
    public UserId(long value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public long Value { get; }
}
