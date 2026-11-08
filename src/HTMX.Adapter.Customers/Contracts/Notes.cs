namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// Free text about a record, such as what was agreed at the last meeting.
/// </summary>
public readonly record struct Notes
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Notes"/> class.
    /// </summary>
    /// <param name="value">The text, which may run to several paragraphs; it must contain something other than white
    /// space.</param>
    public Notes(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the text.
    /// </summary>
    public string Value { get; }
}
