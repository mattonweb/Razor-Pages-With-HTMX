namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// The name of an organization, unique however it is capitalized.
/// </summary>
/// <remarks>
/// Names can be compared with &lt; and &gt;, so a query can ask for the names after another, as key-based paging does.
/// In a query, Entity Framework turns the comparison into SQL on the stored text, where the database compares with the
/// column's NOCASE collation; in C#, two names compare by their characters, as they are equal by them.
/// </remarks>
public readonly record struct OrganizationName : IComparable<OrganizationName>
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

    /// <summary>
    /// Says whether one name sorts before another.
    /// </summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when the first sorts before the second.</returns>
    public static bool operator <(OrganizationName left, OrganizationName right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    /// Says whether one name sorts after another.
    /// </summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when the first sorts after the second.</returns>
    public static bool operator >(OrganizationName left, OrganizationName right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>
    /// Says whether one name sorts before another or is the same.
    /// </summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when the first sorts before the second or is the same.</returns>
    public static bool operator <=(OrganizationName left, OrganizationName right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// Says whether one name sorts after another or is the same.
    /// </summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when the first sorts after the second or is the same.</returns>
    public static bool operator >=(OrganizationName left, OrganizationName right)
    {
        return left.CompareTo(right) >= 0;
    }

    /// <summary>
    /// Compares this name with another, character by character.
    /// </summary>
    /// <param name="other">The other name.</param>
    /// <returns>Less than zero when this name sorts first, zero when they are the same, and more than zero when the
    /// other sorts first.</returns>
    public int CompareTo(OrganizationName other)
    {
        return string.CompareOrdinal(this.Value, other.Value);
    }
}
