namespace HTMX.Adapter.Customers.Services;

using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// The users list's side of key-based paging: which request reads the first page, and the response a page of users
/// becomes. The rules every list shares are <see cref="ListPages"/>'s; <see cref="UserService"/> reads the users and
/// hands them here.
/// </summary>
internal static class UserPages
{
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
    /// Turns the users read into a page in the list's order, with the counts and the cursors for the pages either
    /// side.
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
        ListPage<UserSummary, UserCursor> page = ListPages.Cut(
            read,
            request.Cursor?.Position,
            request.PageSize,
            static (user, side) => new UserCursor(user.Id, side));
        RecordCount all = new RecordCount(active.Value + archived.Value);
        return new UserCollectionResponse
        {
            Request = request,
            Items = page.Items,
            Previous = page.Previous,
            Next = page.Next,
            Matching = ListPages.Matching(request.Status, active, archived, all),
            Active = active,
            Archived = archived,
            All = all,
        };
    }
}
