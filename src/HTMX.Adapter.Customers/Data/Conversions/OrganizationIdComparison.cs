namespace HTMX.Adapter.Customers.Data.Conversions;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Orders organization identifiers by number, so that a query can settle a tie in key-based paging by identifier.
/// </summary>
/// <remarks>
/// Ordering by number is the identifier's own, value semantics, and could sit on <see cref="OrganizationId"/>
/// itself. It sits here, beside <see cref="OrganizationNameComparison"/>, because only the adapter's queries compare
/// identifiers. Entity Framework turns each operator into SQL on the bare Id column.
/// </remarks>
internal static class OrganizationIdComparison
{
    extension(OrganizationId)
    {
        /// <summary>
        /// Says whether one identifier is smaller than another.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns><see langword="true"/> when the first is smaller.</returns>
        public static bool operator <(OrganizationId left, OrganizationId right)
        {
            return left.Value < right.Value;
        }

        /// <summary>
        /// Says whether one identifier is larger than another.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns><see langword="true"/> when the first is larger.</returns>
        public static bool operator >(OrganizationId left, OrganizationId right)
        {
            return left.Value > right.Value;
        }

        /// <summary>
        /// Says whether one identifier is smaller than another or the same.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns><see langword="true"/> when the first is smaller or the same.</returns>
        public static bool operator <=(OrganizationId left, OrganizationId right)
        {
            return left.Value <= right.Value;
        }

        /// <summary>
        /// Says whether one identifier is larger than another or the same.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns><see langword="true"/> when the first is larger or the same.</returns>
        public static bool operator >=(OrganizationId left, OrganizationId right)
        {
            return left.Value >= right.Value;
        }
    }
}
