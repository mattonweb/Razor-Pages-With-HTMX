namespace HTMX.Adapter.Customers.Services;

using System.Linq;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The rules of key-based paging that need no query: when a cursor has left too little to show, which request reads
/// the first page instead, and how the users read become a page with the cursors either side of it.
/// <see cref="UserService"/> reads the users and hands them here.
/// </summary>
internal static class UserPages
{
    /// <summary>
    /// Says whether a cursor left too little to show: nothing at all past it, or a short page before it, which means
    /// the page reached the start of the list, and the first page, read forwards, should be shown whole instead.
    /// </summary>
    /// <param name="request">The request that was read.</param>
    /// <param name="read">The users read for it.</param>
    /// <returns><see langword="true"/> when the first page should be read instead.</returns>
    internal static bool IsTooShort(UserCollectionRequest request, UserSummary[] read)
    {
        bool backwards = request.Cursor?.Position == CursorPosition.Before;
        return read.Length == 0 || (backwards && read.Length < request.PageSize.Value);
    }

    /// <summary>
    /// Makes the request for the first page of the same list: the same filter, status, sort and size, and no cursor.
    /// </summary>
    /// <param name="request">The request to start again from.</param>
    /// <returns>The request for the first page.</returns>
    internal static UserCollectionRequest FirstPage(UserCollectionRequest request)
    {
        return request with { Cursor = null };
    }

    /// <summary>
    /// Turns the users read into a page in the list's order, with the counts and the cursors for the pages
    /// either side. The extra user read, if any, is dropped; it only showed that another page exists.
    /// </summary>
    /// <param name="request">The request the page answers.</param>
    /// <param name="read">The users read for it, in the order they were read.</param>
    /// <param name="active">How many active users match the filter.</param>
    /// <param name="archived">How many archived users match the filter.</param>
    /// <returns>The page.</returns>
    internal static UserCollectionResponse CreatePage(
        UserCollectionRequest request,
        UserSummary[] read,
        RecordCount active,
        RecordCount archived)
    {
        bool backwards = request.Cursor?.Position == CursorPosition.Before;
        bool more = read.Length > request.PageSize.Value;
        UserSummary[] shown = [.. read.Take(request.PageSize.Value)];
        UserSummary[] items = backwards ? [.. Enumerable.Reverse(shown)] : shown;

        // Read forwards, the extra user means a next page, and any cursor means a previous one. Read
        // backwards, it is the other way round.
        bool hasPrevious = backwards ? more : request.Cursor is not null;
        bool hasNext = backwards || more;
        RecordCount all = new RecordCount(active.Value + archived.Value);
        return new UserCollectionResponse
        {
            Request = request,
            Items = items,
            Previous = hasPrevious ? Edge(items, 0, CursorPosition.Before) : null,
            Next = hasNext ? Edge(items, items.Length - 1, CursorPosition.After) : null,
            Matching = Matching(request.Status, active, archived, all),
            Active = active,
            Archived = archived,
            All = all,
        };
    }

    /// <summary>
    /// Picks the count that matches the list's status.
    /// </summary>
    /// <param name="status">Which users the list shows.</param>
    /// <param name="active">How many active users match the filter.</param>
    /// <param name="archived">How many archived users match the filter.</param>
    /// <param name="all">How many users match the filter.</param>
    /// <returns>How many users the list holds, on every page together.</returns>
    private static RecordCount Matching(RecordStatus status, RecordCount active, RecordCount archived, RecordCount all)
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
    /// Makes the cursor for the page beside one of a page's users.
    /// </summary>
    /// <param name="items">The page's users.</param>
    /// <param name="index">Which user: the first, for the page before, or the last, for the page after.</param>
    /// <param name="position">Which side of that user the other page lies on.</param>
    /// <returns>The cursor, or <see langword="null"/> when the page is empty.</returns>
    private static UserCursor? Edge(UserSummary[] items, int index, CursorPosition position)
    {
        if (items.Length == 0)
        {
            return null;
        }

        return new UserCursor(items[index].Id, position);
    }
}
