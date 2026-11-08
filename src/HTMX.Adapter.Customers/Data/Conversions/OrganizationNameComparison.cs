namespace HTMX.Adapter.Customers.Data.Conversions;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Orders organization names as the Customers database does, so that a query can ask for the names after another, as
/// key-based paging does.
/// </summary>
/// <remarks>
/// This is the database's ordering, not the name's own: the Name column uses SQLite's NOCASE collation, which ignores
/// the case of A to Z, and another store or culture would order names differently. That makes it context semantics,
/// so it lives here at the data layer's boundary, beside the value converters, and is internal: nothing outside the
/// adapter can order names by the database's rules by mistake. The operators are C# 14 extension operators, which add
/// <c>&lt;</c> and <c>&gt;</c> to <see cref="OrganizationName"/> without putting them on the type. In a query, Entity
/// Framework turns each one into SQL on the bare Name column, which can seek in the column's index. In memory they
/// compare ignoring case; SQLite folds only A to Z, so a name with an accented capital may sort differently here.
/// </remarks>
internal static class OrganizationNameComparison
{
    extension(OrganizationName)
    {
        /// <summary>
        /// Says whether one name sorts before another.
        /// </summary>
        /// <param name="left">The first name.</param>
        /// <param name="right">The second name.</param>
        /// <returns><see langword="true"/> when the first sorts before the second.</returns>
        public static bool operator <(OrganizationName left, OrganizationName right)
        {
            return Compare(left, right) < 0;
        }

        /// <summary>
        /// Says whether one name sorts after another.
        /// </summary>
        /// <param name="left">The first name.</param>
        /// <param name="right">The second name.</param>
        /// <returns><see langword="true"/> when the first sorts after the second.</returns>
        public static bool operator >(OrganizationName left, OrganizationName right)
        {
            return Compare(left, right) > 0;
        }

        /// <summary>
        /// Says whether one name sorts before another or with it.
        /// </summary>
        /// <param name="left">The first name.</param>
        /// <param name="right">The second name.</param>
        /// <returns><see langword="true"/> when the first sorts before the second or with it.</returns>
        public static bool operator <=(OrganizationName left, OrganizationName right)
        {
            return Compare(left, right) <= 0;
        }

        /// <summary>
        /// Says whether one name sorts after another or with it.
        /// </summary>
        /// <param name="left">The first name.</param>
        /// <param name="right">The second name.</param>
        /// <returns><see langword="true"/> when the first sorts after the second or with it.</returns>
        public static bool operator >=(OrganizationName left, OrganizationName right)
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
    private static int Compare(OrganizationName left, OrganizationName right)
    {
        return string.Compare(left.Value, right.Value, StringComparison.OrdinalIgnoreCase);
    }
}
