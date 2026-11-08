namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// A person's job title at their organization, such as Head of Examples.
/// </summary>
public readonly record struct JobTitle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobTitle"/> class.
    /// </summary>
    /// <param name="value">The title; it must contain something other than white space.</param>
    public JobTitle(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the title.
    /// </summary>
    public string Value { get; }
}
