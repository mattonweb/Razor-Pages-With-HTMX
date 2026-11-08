namespace HTMX.Adapter.Contacts.Contracts;

using System;

/// <summary>
/// The name of an organization, unique however it is capitalized.
/// </summary>
public readonly record struct OrganizationName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationName"/> class.
    /// </summary>
    /// <param name="value">The name; it must contain something other than white space.</param>
    public OrganizationName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Value { get; }
}
