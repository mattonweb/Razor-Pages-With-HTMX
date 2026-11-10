namespace HTMX.Application.Web.Pages;

using System;
using System.Collections.Generic;
using System.Globalization;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The query string every list page keeps its view in: the names of its values, the page sizes it offers, and how a
/// value is read from it and written to it. A value the page does not recognize falls back to its default, so a
/// mistyped URL still shows a list, and a value that is the default is left out, so the default view's URL stays plain.
/// </summary>
public static class ListQuery
{
    /// <summary>
    /// The query string name of the text the list's records must contain. Every name is short and lower case, like the
    /// URLs the application writes.
    /// </summary>
    public const string FilterKey = "filter";

    /// <summary>
    /// The query string name of which records to show: active, archived or all.
    /// </summary>
    public const string StatusKey = "status";

    /// <summary>
    /// The query string name of the column to sort by.
    /// </summary>
    public const string SortKey = "sort";

    /// <summary>
    /// The query string name of which way to sort: ascending or descending.
    /// </summary>
    public const string DirectionKey = "dir";

    /// <summary>
    /// The query string name of the record the page starts after: where Next goes.
    /// </summary>
    public const string AfterKey = "after";

    /// <summary>
    /// The query string name of the record the page ends before: where Previous goes.
    /// </summary>
    public const string BeforeKey = "before";

    /// <summary>
    /// The query string name of how many records a page holds.
    /// </summary>
    public const string PageSizeKey = "size";

    /// <summary>
    /// How many records a page holds until the Rows per page select says otherwise.
    /// </summary>
    public const int DefaultPageSize = 10;

    private static readonly int[] Sizes = [DefaultPageSize, 25, 50, PageSize.Largest];

    /// <summary>
    /// Gets the page sizes the Rows per page select offers.
    /// </summary>
    public static IReadOnlyList<int> PageSizes => Sizes;

    /// <summary>
    /// Reads a choice from the query string, ignoring case, or gives the default when it is missing or unknown.
    /// </summary>
    /// <typeparam name="TChoice">The list of choices.</typeparam>
    /// <param name="text">The value as sent.</param>
    /// <param name="fallback">The default.</param>
    /// <returns>The choice.</returns>
    public static TChoice ParseOrDefault<TChoice>(string? text, TChoice fallback)
        where TChoice : struct, Enum
    {
        return Enum.TryParse(text, ignoreCase: true, out TChoice parsed) && Enum.IsDefined(parsed) ? parsed : fallback;
    }

    /// <summary>
    /// Reads the page size from the query string, or gives the default when it is missing or not one the select offers.
    /// </summary>
    /// <param name="size">The page size as sent, if any.</param>
    /// <returns>The page size.</returns>
    public static PageSize ChoosePageSize(int? size)
    {
        if (size is int offered && Array.IndexOf(Sizes, offered) >= 0)
        {
            return new PageSize(offered);
        }

        return new PageSize(DefaultPageSize);
    }

    /// <summary>
    /// Says which way a sort header's link sorts. The sorted column's link turns the order round; another column's link
    /// sorts by it in its natural order.
    /// </summary>
    /// <typeparam name="TColumn">The list's columns.</typeparam>
    /// <param name="column">The header's column.</param>
    /// <param name="sorted">The column the list is sorted by now.</param>
    /// <param name="direction">Which way the list is sorted now.</param>
    /// <param name="natural">Which way the header's column sorts when it is first chosen.</param>
    /// <returns>The direction the link asks for.</returns>
    public static SortDirection NextDirection<TColumn>(
        TColumn column,
        TColumn sorted,
        SortDirection direction,
        SortDirection natural)
        where TColumn : struct, Enum
    {
        if (!EqualityComparer<TColumn>.Default.Equals(column, sorted))
        {
            return natural;
        }

        return direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
    }

    /// <summary>
    /// Says how a column's header is sorted, for its <c>aria-sort</c> attribute.
    /// </summary>
    /// <typeparam name="TColumn">The list's columns.</typeparam>
    /// <param name="column">The header's column.</param>
    /// <param name="sorted">The column the list is sorted by.</param>
    /// <param name="direction">Which way the list is sorted.</param>
    /// <returns><c>ascending</c> or <c>descending</c> for the sorted column, and <see langword="null"/> for the
    /// others, which leaves the attribute out.</returns>
    public static string? AriaSort<TColumn>(TColumn column, TColumn sorted, SortDirection direction)
        where TColumn : struct, Enum
    {
        return EqualityComparer<TColumn>.Default.Equals(column, sorted) ? Name(direction) : null;
    }

    /// <summary>
    /// Adds a value to a query string unless it is missing or the default, so the default view's URL stays plain.
    /// </summary>
    /// <param name="values">The query string being built.</param>
    /// <param name="key">The value's name.</param>
    /// <param name="value">The value, if any.</param>
    /// <param name="defaultValue">The value that is left out.</param>
    public static void AddUnlessDefault(
        Dictionary<string, string> values,
        string key,
        string? value,
        string? defaultValue)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (value is null || value == defaultValue)
        {
            return;
        }

        values[key] = value;
    }

    /// <summary>
    /// Writes a choice as it appears in a URL, such as archived.
    /// </summary>
    /// <param name="choice">The choice.</param>
    /// <returns>Its name in lower case.</returns>
    public static string Name(Enum choice)
    {
        ArgumentNullException.ThrowIfNull(choice);
        string name = choice.ToString();
        return name.ToLowerInvariant();
    }

    /// <summary>
    /// Writes a number as it appears in a URL, the same whatever the browser's language.
    /// </summary>
    /// <param name="number">The number, such as a page size or a record's identifier.</param>
    /// <returns>The number in the invariant culture.</returns>
    public static string Number(long number)
    {
        return number.ToString(CultureInfo.InvariantCulture);
    }
}
