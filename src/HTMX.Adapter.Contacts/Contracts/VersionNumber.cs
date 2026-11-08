namespace HTMX.Adapter.Contacts.Contracts;

using System;

/// <summary>
/// The number of one version of a record, shown as v5; every change makes the next one.
/// </summary>
public readonly record struct VersionNumber
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VersionNumber"/> class.
    /// </summary>
    /// <param name="value">The number, counting from 1 for the version that created the record.</param>
    public VersionNumber(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the number.
    /// </summary>
    public int Value { get; }
}
