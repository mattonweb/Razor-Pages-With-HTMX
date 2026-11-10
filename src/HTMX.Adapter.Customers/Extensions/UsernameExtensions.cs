namespace HTMX.Adapter.Customers.Extensions;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The adapter's extension methods for <see cref="Username"/>: how usernames order in the Users table, so that a
/// query can ask for the usernames after another, as key-based paging does.
/// </summary>
/// <remarks>
/// The same arrangement as <see cref="DisplayNameExtensions"/>: this is the database's ordering, context semantics,
/// so it lives in the adapter and is internal. The Users table's Username column uses SQLite's NOCASE collation, which
/// ignores the case of A to Z, and these methods compare the same way. In a query, Entity Framework turns each one into
/// a plain comparison on the bare Username column, which can seek in the column's unique index; SQLite ends every
/// index entry with the row's identifier, so that index also settles a tie.
/// </remarks>
internal static class UsernameExtensions
{
    /// <summary>
    /// Says whether one username sorts before another.
    /// </summary>
    /// <param name="left">The first username.</param>
    /// <param name="right">The second username.</param>
    /// <returns><see langword="true"/> when the first sorts before the second.</returns>
    public static bool IsLessThan(this Username left, Username right)
    {
        return Compare(left, right) < 0;
    }

    /// <summary>
    /// Says whether one username sorts after another.
    /// </summary>
    /// <param name="left">The first username.</param>
    /// <param name="right">The second username.</param>
    /// <returns><see langword="true"/> when the first sorts after the second.</returns>
    public static bool IsGreaterThan(this Username left, Username right)
    {
        return Compare(left, right) > 0;
    }

    /// <summary>
    /// Says whether one username sorts before another or with it.
    /// </summary>
    /// <param name="left">The first username.</param>
    /// <param name="right">The second username.</param>
    /// <returns><see langword="true"/> when the first sorts before the second or with it.</returns>
    public static bool IsLessThanOrEqualTo(this Username left, Username right)
    {
        return Compare(left, right) <= 0;
    }

    /// <summary>
    /// Says whether one username sorts after another or with it.
    /// </summary>
    /// <param name="left">The first username.</param>
    /// <param name="right">The second username.</param>
    /// <returns><see langword="true"/> when the first sorts after the second or with it.</returns>
    public static bool IsGreaterThanOrEqualTo(this Username left, Username right)
    {
        return Compare(left, right) >= 0;
    }

    /// <summary>
    /// Compares two usernames ignoring case, as the Username column's collation does.
    /// </summary>
    /// <param name="left">The first username.</param>
    /// <param name="right">The second username.</param>
    /// <returns>Less than zero when the first sorts first, zero when they sort together, and more than zero when the
    /// second sorts first.</returns>
    private static int Compare(Username left, Username right)
    {
        return string.Compare(left.Value, right.Value, StringComparison.OrdinalIgnoreCase);
    }
}
