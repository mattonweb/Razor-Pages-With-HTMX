namespace HTMX.Adapter.Customers.Extensions;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The adapter's extension methods for <see cref="DisplayName"/>: how contacts' and users' names order in their
/// tables, so that a query can ask for the names after another, as key-based paging does.
/// </summary>
/// <remarks>
/// This is the database's ordering, not the name's own: the Contacts table's Name column and the Users table's
/// DisplayName column both use SQLite's NOCASE collation, which ignores the case of A to Z, and another store or
/// culture would order names differently. That makes it context semantics, so it lives in the adapter, internal, and
/// not on the contract type. In a query, Entity Framework turns each method into a plain comparison on the bare column,
/// as <see cref="Data.ComparisonTranslations"/> teaches it, which can seek in the index that starts with the column. In
/// memory the methods compare ignoring case; SQLite folds only A to Z, so a name with an accented capital may sort
/// differently there.
/// </remarks>
internal static class DisplayNameExtensions
{
    /// <summary>
    /// Says whether one name sorts before another.
    /// </summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when the first sorts before the second.</returns>
    public static bool IsLessThan(this DisplayName left, DisplayName right)
    {
        return Compare(left, right) < 0;
    }

    /// <summary>
    /// Says whether one name sorts after another.
    /// </summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when the first sorts after the second.</returns>
    public static bool IsGreaterThan(this DisplayName left, DisplayName right)
    {
        return Compare(left, right) > 0;
    }

    /// <summary>
    /// Says whether one name sorts before another or with it.
    /// </summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when the first sorts before the second or with it.</returns>
    public static bool IsLessThanOrEqualTo(this DisplayName left, DisplayName right)
    {
        return Compare(left, right) <= 0;
    }

    /// <summary>
    /// Says whether one name sorts after another or with it.
    /// </summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when the first sorts after the second or with it.</returns>
    public static bool IsGreaterThanOrEqualTo(this DisplayName left, DisplayName right)
    {
        return Compare(left, right) >= 0;
    }

    /// <summary>
    /// Compares two names ignoring case, as the columns' collation does.
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
