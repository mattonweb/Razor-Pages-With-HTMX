namespace HTMX.Adapter.Customers.Data.Conversions;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Orders contacts' names as the Contacts table does, so that a query can ask for the names after another, as
/// key-based paging does.
/// </summary>
/// <remarks>
/// The same arrangement as <see cref="OrganizationNameComparison"/>, for the same reasons: this is the database's
/// ordering, context semantics, so it lives at the data layer's boundary and is internal. The Contacts table's Name
/// column uses SQLite's NOCASE collation, which ignores the case of A to Z. A user's display name is a
/// <see cref="DisplayName"/> too, but the Users table compares it with case, so these operators are only for the
/// contacts' names. In a query, Entity Framework turns each one into SQL on the bare Name column, which can seek in
/// the index on the organization, the name and the identifier.
/// </remarks>
internal static class DisplayNameComparison
{
    extension(DisplayName)
    {
        /// <summary>
        /// Says whether one name sorts before another.
        /// </summary>
        /// <param name="left">The first name.</param>
        /// <param name="right">The second name.</param>
        /// <returns><see langword="true"/> when the first sorts before the second.</returns>
        public static bool operator <(DisplayName left, DisplayName right)
        {
            return Compare(left, right) < 0;
        }

        /// <summary>
        /// Says whether one name sorts after another.
        /// </summary>
        /// <param name="left">The first name.</param>
        /// <param name="right">The second name.</param>
        /// <returns><see langword="true"/> when the first sorts after the second.</returns>
        public static bool operator >(DisplayName left, DisplayName right)
        {
            return Compare(left, right) > 0;
        }

        /// <summary>
        /// Says whether one name sorts before another or with it.
        /// </summary>
        /// <param name="left">The first name.</param>
        /// <param name="right">The second name.</param>
        /// <returns><see langword="true"/> when the first sorts before the second or with it.</returns>
        public static bool operator <=(DisplayName left, DisplayName right)
        {
            return Compare(left, right) <= 0;
        }

        /// <summary>
        /// Says whether one name sorts after another or with it.
        /// </summary>
        /// <param name="left">The first name.</param>
        /// <param name="right">The second name.</param>
        /// <returns><see langword="true"/> when the first sorts after the second or with it.</returns>
        public static bool operator >=(DisplayName left, DisplayName right)
        {
            return Compare(left, right) >= 0;
        }
    }

    /// <summary>
    /// Compares two names ignoring case, as the Name column's collation does.
    /// </summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns>Less than zero when the first sorts first, zero when they sort together, and more than zero when the
    /// second sorts first.</returns>
    private static int Compare(DisplayName left, DisplayName right)
    {
        return string.Compare(left.Value, right.Value, StringComparison.OrdinalIgnoreCase);
    }
}
