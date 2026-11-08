namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// Identifies one organization.
/// </summary>
/// <remarks>
/// Unlike the other wrapped values, an identifier is not checked when it is made. Entity Framework gives a new
/// organization a negative placeholder identifier until the database assigns the real one, and it builds that
/// placeholder through this constructor. Identifiers can be compared with &lt; and &gt;, so a query can settle a tie
/// in key-based paging by identifier; Entity Framework turns the comparison into SQL on the stored number.
/// </remarks>
public readonly record struct OrganizationId : IComparable<OrganizationId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationId"/> class.
    /// </summary>
    /// <param name="value">The identifier the database gave the organization.</param>
    public OrganizationId(long value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Says whether one identifier is smaller than another.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns><see langword="true"/> when the first is smaller.</returns>
    public static bool operator <(OrganizationId left, OrganizationId right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    /// Says whether one identifier is larger than another.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns><see langword="true"/> when the first is larger.</returns>
    public static bool operator >(OrganizationId left, OrganizationId right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>
    /// Says whether one identifier is smaller than another or the same.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns><see langword="true"/> when the first is smaller or the same.</returns>
    public static bool operator <=(OrganizationId left, OrganizationId right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// Says whether one identifier is larger than another or the same.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns><see langword="true"/> when the first is larger or the same.</returns>
    public static bool operator >=(OrganizationId left, OrganizationId right)
    {
        return left.CompareTo(right) >= 0;
    }

    /// <summary>
    /// Compares this identifier with another.
    /// </summary>
    /// <param name="other">The other identifier.</param>
    /// <returns>Less than zero when this one is smaller, zero when they are the same, and more than zero when it is
    /// larger.</returns>
    public int CompareTo(OrganizationId other)
    {
        return this.Value.CompareTo(other.Value);
    }
}
