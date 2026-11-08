namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// The name a user is known by in the application, unique however it is capitalized.
/// </summary>
public readonly record struct Username
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Username"/> class.
    /// </summary>
    /// <param name="value">The name; it must contain something other than white space.</param>
    public Username(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Value { get; }
}
