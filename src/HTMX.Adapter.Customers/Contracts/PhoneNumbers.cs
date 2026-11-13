namespace HTMX.Adapter.Customers.Contracts;

using System;
using System.Linq;

/// <summary>
/// A person's telephone numbers, in the order they were written, the first being the one to try first. Two are equal
/// when they hold the same numbers in the same order, so a save that changes none of them is seen to change nothing.
/// </summary>
public sealed record PhoneNumbers
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PhoneNumbers"/> class.
    /// </summary>
    /// <param name="values">The numbers, in order; the list keeps a copy.</param>
    public PhoneNumbers(PhoneNumber[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        this.Values = [.. values];
    }

    /// <summary>
    /// Gets no numbers, for a person with none on record.
    /// </summary>
    public static PhoneNumbers None => new PhoneNumbers([]);

    /// <summary>
    /// Gets the numbers, in order.
    /// </summary>
    public PhoneNumber[] Values { get; }

    /// <summary>
    /// Says whether another list holds the same numbers in the same order.
    /// </summary>
    /// <param name="other">The other list.</param>
    /// <returns><see langword="true"/> when the numbers and their order are the same.</returns>
    public bool Equals(PhoneNumbers? other)
    {
        return other is not null && this.Values.SequenceEqual(other.Values);
    }

    /// <summary>
    /// Combines the numbers, in order, so that equal lists give the same hash.
    /// </summary>
    /// <returns>The hash.</returns>
    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (PhoneNumber number in this.Values)
        {
            hash.Add(number);
        }

        return hash.ToHashCode();
    }
}
