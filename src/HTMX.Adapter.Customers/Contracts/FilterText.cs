namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// Text a list is narrowed by: a record matches when its name contains the text, however it is capitalized.
/// </summary>
public readonly record struct FilterText
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterText"/> class.
    /// </summary>
    /// <param name="value">The text, as typed; it must contain something other than white space.</param>
    public FilterText(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value.Trim();
    }

    /// <summary>
    /// Gets the text, without leading or trailing white space.
    /// </summary>
    public string Value { get; }
}
