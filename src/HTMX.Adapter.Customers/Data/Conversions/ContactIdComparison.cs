namespace HTMX.Adapter.Customers.Data.Conversions;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Orders contact identifiers by number, so that a query can settle a tie in key-based paging by identifier.
/// </summary>
/// <remarks>
/// As <see cref="OrganizationIdComparison"/> does for organizations. Entity Framework turns each operator into SQL on
/// the bare Id column.
/// </remarks>
internal static class ContactIdComparison
{
    extension(ContactId)
    {
        /// <summary>
        /// Says whether one identifier is smaller than another.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns><see langword="true"/> when the first is smaller.</returns>
        public static bool operator <(ContactId left, ContactId right)
        {
            return left.Value < right.Value;
        }

        /// <summary>
        /// Says whether one identifier is larger than another.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns><see langword="true"/> when the first is larger.</returns>
        public static bool operator >(ContactId left, ContactId right)
        {
            return left.Value > right.Value;
        }

        /// <summary>
        /// Says whether one identifier is smaller than another or the same.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns><see langword="true"/> when the first is smaller or the same.</returns>
        public static bool operator <=(ContactId left, ContactId right)
        {
            return left.Value <= right.Value;
        }

        /// <summary>
        /// Says whether one identifier is larger than another or the same.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns><see langword="true"/> when the first is larger or the same.</returns>
        public static bool operator >=(ContactId left, ContactId right)
        {
            return left.Value >= right.Value;
        }
    }
}
