namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// How many records there are of some kind, such as the contacts of one organization or the archived records in a
/// list.
/// </summary>
public readonly record struct RecordCount
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordCount"/> class.
    /// </summary>
    /// <param name="value">The count, from 0.</param>
    public RecordCount(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    public int Value { get; }
}
