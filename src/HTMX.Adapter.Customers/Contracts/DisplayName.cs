namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// A person's name as the application shows it, such as "Updated today by Demo User".
/// </summary>
public readonly record struct DisplayName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayName"/> class.
    /// </summary>
    /// <param name="value">The name; it must contain something other than white space.</param>
    public DisplayName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Value { get; }
}
