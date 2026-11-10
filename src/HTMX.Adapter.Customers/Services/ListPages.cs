namespace HTMX.Adapter.Customers.Services;

using System;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// The rules of key-based paging that need no query, the same for every list: when a cursor has left too little to
/// show, how the records read become a page with the cursors either side of it, and which count a list's status picks.
/// A service reads the records and hands them here; each list's own pages class, such as
/// <see cref="OrganizationPages"/>, makes its response from the page.
/// </summary>
internal static class ListPages
{
    /// <summary>
    /// Says whether a cursor left too little to show: nothing at all past it, or a short page before it, which means
    /// the page reached the start of the list, and the first page, read forwards, should be shown whole instead.
    /// </summary>
    /// <typeparam name="TItem">The kind of record the list shows.</typeparam>
    /// <param name="position">Which side of its record the cursor read, or <see langword="null"/> for no
    /// cursor.</param>
    /// <param name="size">How many records a page shows.</param>
    /// <param name="read">The records read for the page.</param>
    /// <returns><see langword="true"/> when the first page should be read instead.</returns>
    internal static bool IsTooShort<TItem>(CursorPosition? position, PageSize size, TItem[] read)
    {
        ArgumentNullException.ThrowIfNull(read);
        bool backwards = position == CursorPosition.Before;
        return read.Length == 0 || (backwards && read.Length < size.Value);
    }

    /// <summary>
    /// Turns the records read into a page in the list's order, with the cursors for the pages either side. A page is
    /// read one record longer than it shows; that extra record, if any, is dropped, since it only showed that another
    /// page exists.
    /// </summary>
    /// <typeparam name="TItem">The kind of record the list shows.</typeparam>
    /// <typeparam name="TCursor">The cursor that names one of those records.</typeparam>
    /// <param name="read">The records read for the page, in the order they were read.</param>
    /// <param name="position">Which side of its record the cursor read, or <see langword="null"/> for no
    /// cursor.</param>
    /// <param name="size">How many records a page shows.</param>
    /// <param name="cursorBeside">Makes the cursor for the page on one side of a record.</param>
    /// <returns>The page.</returns>
    internal static ListPage<TItem, TCursor> Cut<TItem, TCursor>(
        TItem[] read,
        CursorPosition? position,
        PageSize size,
        Func<TItem, CursorPosition, TCursor> cursorBeside)
        where TCursor : struct
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(cursorBeside);
        bool backwards = position == CursorPosition.Before;
        bool more = read.Length > size.Value;
        TItem[] shown = [.. read.Take(size.Value)];
        TItem[] items = backwards ? [.. Enumerable.Reverse(shown)] : shown;

        // Read forwards, the extra record means a next page, and any cursor means a previous one. Read backwards, it is
        // the other way round.
        bool hasPrevious = backwards ? more : position is not null;
        bool hasNext = backwards || more;
        return new ListPage<TItem, TCursor>
        {
            Items = items,
            Previous = hasPrevious ? Edge(items, 0, CursorPosition.Before, cursorBeside) : null,
            Next = hasNext ? Edge(items, items.Length - 1, CursorPosition.After, cursorBeside) : null,
        };
    }

    /// <summary>
    /// Picks the count that matches a list's status.
    /// </summary>
    /// <param name="status">Which records the list shows.</param>
    /// <param name="active">How many active records match the filter.</param>
    /// <param name="archived">How many archived records match the filter.</param>
    /// <param name="all">How many records match the filter.</param>
    /// <returns>How many records the list holds, on every page together.</returns>
    internal static RecordCount Matching(RecordStatus status, RecordCount active, RecordCount archived, RecordCount all)
    {
        return status switch
        {
            RecordStatus.Active =>
                active,
            RecordStatus.Archived =>
                archived,
            _ =>
                all,
        };
    }

    /// <summary>
    /// Makes the cursor for the page beside one of a page's records.
    /// </summary>
    /// <typeparam name="TItem">The kind of record the list shows.</typeparam>
    /// <typeparam name="TCursor">The cursor that names one of those records.</typeparam>
    /// <param name="items">The page's records.</param>
    /// <param name="index">Which record: the first, for the page before, or the last, for the page after.</param>
    /// <param name="position">Which side of that record the other page lies on.</param>
    /// <param name="cursorBeside">Makes the cursor for the page on one side of a record.</param>
    /// <returns>The cursor, or <see langword="null"/> when the page is empty.</returns>
    private static TCursor? Edge<TItem, TCursor>(
        TItem[] items,
        int index,
        CursorPosition position,
        Func<TItem, CursorPosition, TCursor> cursorBeside)
        where TCursor : struct
    {
        if (items.Length == 0)
        {
            return null;
        }

        return cursorBeside(items[index], position);
    }
}
