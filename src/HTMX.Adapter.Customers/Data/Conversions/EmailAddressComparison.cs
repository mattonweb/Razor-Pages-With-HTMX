namespace HTMX.Adapter.Customers.Data.Conversions;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Orders email addresses as the Users table does, so that a query can ask for the email addresses after another,
/// as key-based paging does.
/// </summary>
/// <remarks>
/// The same arrangement as <see cref="DisplayNameComparison"/>: this is the database's ordering, context semantics,
/// so it lives at the data layer's boundary and is internal. The Users table's Email column uses SQLite's NOCASE
/// collation, which ignores the case of A to Z, and these operators compare the same way. In a query, Entity Framework
/// turns each one into SQL on the bare Email column, which can seek in the column's unique index; SQLite ends every
/// index entry with the row's identifier, so that index also settles a tie.
/// </remarks>
internal static class EmailAddressComparison
{
    extension(EmailAddress)
    {
        /// <summary>
        /// Says whether one email address sorts before another.
        /// </summary>
        /// <param name="left">The first email address.</param>
        /// <param name="right">The second email address.</param>
        /// <returns><see langword="true"/> when the first sorts before the second.</returns>
        public static bool operator <(EmailAddress left, EmailAddress right)
        {
            return Compare(left, right) < 0;
        }

        /// <summary>
        /// Says whether one email address sorts after another.
        /// </summary>
        /// <param name="left">The first email address.</param>
        /// <param name="right">The second email address.</param>
        /// <returns><see langword="true"/> when the first sorts after the second.</returns>
        public static bool operator >(EmailAddress left, EmailAddress right)
        {
            return Compare(left, right) > 0;
        }

        /// <summary>
        /// Says whether one email address sorts before another or with it.
        /// </summary>
        /// <param name="left">The first email address.</param>
        /// <param name="right">The second email address.</param>
        /// <returns><see langword="true"/> when the first sorts before the second or with it.</returns>
        public static bool operator <=(EmailAddress left, EmailAddress right)
        {
            return Compare(left, right) <= 0;
        }

        /// <summary>
        /// Says whether one email address sorts after another or with it.
        /// </summary>
        /// <param name="left">The first email address.</param>
        /// <param name="right">The second email address.</param>
        /// <returns><see langword="true"/> when the first sorts after the second or with it.</returns>
        public static bool operator >=(EmailAddress left, EmailAddress right)
        {
            return Compare(left, right) >= 0;
        }
    }

    /// <summary>
    /// Compares two email addresses ignoring case, as the Email column's collation does.
    /// </summary>
    /// <param name="left">The first email address.</param>
    /// <param name="right">The second email address.</param>
    /// <returns>Less than zero when the first sorts first, zero when they sort together, and more than zero when the
    /// second sorts first.</returns>
    private static int Compare(EmailAddress left, EmailAddress right)
    {
        return string.Compare(left.Value, right.Value, StringComparison.OrdinalIgnoreCase);
    }
}
