namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// Which page of a list to show, counting from 1.
/// </summary>
public readonly record struct PageNumber
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PageNumber"/> class.
    /// </summary>
    /// <param name="value">The page, from 1.</param>
    public PageNumber(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the page.
    /// </summary>
    public int Value { get; }
}
