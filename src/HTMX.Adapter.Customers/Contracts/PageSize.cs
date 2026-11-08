namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// How many records one page of a list holds: at least 1 and at most <see cref="Largest"/>.
/// </summary>
public readonly record struct PageSize
{
    /// <summary>
    /// The most records one page may hold, so no request reads a whole table.
    /// </summary>
    public const int Largest = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="PageSize"/> class.
    /// </summary>
    /// <param name="value">The number of records, from 1 to <see cref="Largest"/>.</param>
    public PageSize(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Largest);
        this.Value = value;
    }

    /// <summary>
    /// Gets the number of records.
    /// </summary>
    public int Value { get; }
}
