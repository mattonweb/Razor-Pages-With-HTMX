namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// The address of a website.
/// </summary>
public readonly record struct WebAddress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebAddress"/> class.
    /// </summary>
    /// <param name="value">The address, such as https://placeholder.example; it must contain something other than white space.</param>
    public WebAddress(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the address.
    /// </summary>
    public string Value { get; }
}
