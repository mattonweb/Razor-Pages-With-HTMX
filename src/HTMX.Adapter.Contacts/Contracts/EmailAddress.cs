namespace HTMX.Adapter.Contacts.Contracts;

using System;

/// <summary>
/// An email address, unique to one user however it is capitalized.
/// </summary>
public readonly record struct EmailAddress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailAddress"/> class.
    /// </summary>
    /// <param name="value">The address; it must contain something other than white space.</param>
    public EmailAddress(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the address.
    /// </summary>
    public string Value { get; }
}
