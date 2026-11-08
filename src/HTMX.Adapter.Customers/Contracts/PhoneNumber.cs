namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// A telephone number, kept as it was written.
/// </summary>
public readonly record struct PhoneNumber
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PhoneNumber"/> class.
    /// </summary>
    /// <param name="value">The number as written, such as +1 555-0142; it must contain something other than white
    /// space.</param>
    public PhoneNumber(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the number.
    /// </summary>
    public string Value { get; }
}
