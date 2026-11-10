namespace HTMX.Adapter.Customers.Extensions;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The adapter's extension methods for <see cref="OrganizationId"/>: how organization identifiers order, so that a
/// query can settle a tie in key-based paging by identifier.
/// </summary>
/// <remarks>
/// Only the adapter's queries compare identifiers, so the methods live here, internal, and not on the contract type.
/// In a query, Entity Framework turns each one into a plain comparison on the bare Id column, as
/// <see cref="Data.ComparisonTranslations"/> teaches it; in memory they compare the numbers.
/// </remarks>
internal static class OrganizationIdExtensions
{
    /// <summary>
    /// Says whether one identifier is smaller than another.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns><see langword="true"/> when the first is smaller.</returns>
    public static bool IsLessThan(this OrganizationId left, OrganizationId right)
    {
        return left.Value < right.Value;
    }

    /// <summary>
    /// Says whether one identifier is larger than another.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns><see langword="true"/> when the first is larger.</returns>
    public static bool IsGreaterThan(this OrganizationId left, OrganizationId right)
    {
        return left.Value > right.Value;
    }

    /// <summary>
    /// Says whether one identifier is smaller than another or the same.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns><see langword="true"/> when the first is smaller or the same.</returns>
    public static bool IsLessThanOrEqualTo(this OrganizationId left, OrganizationId right)
    {
        return left.Value <= right.Value;
    }

    /// <summary>
    /// Says whether one identifier is larger than another or the same.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns><see langword="true"/> when the first is larger or the same.</returns>
    public static bool IsGreaterThanOrEqualTo(this OrganizationId left, OrganizationId right)
    {
        return left.Value >= right.Value;
    }
}
