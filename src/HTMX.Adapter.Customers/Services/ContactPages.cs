namespace HTMX.Adapter.Customers.Services;

using System.Linq;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The rules of key-based paging over an organization's contacts that need no query, as
/// <see cref="OrganizationPages"/> holds them for the organizations list: when a cursor has left too little to show,
/// and how the contacts read become a page with the cursors either side of it.
/// </summary>
internal static class ContactPages
{
    /// <summary>
    /// Says whether a cursor left too little to show: nothing at all past it, or a short page before it, which means
    /// the page reached the start of the list, and the first page, read forwards, should be shown whole instead.
    /// </summary>
    /// <param name="request">The request that was read.</param>
    /// <param name="read">The contacts read for it.</param>
    /// <returns><see langword="true"/> when the first page should be read instead.</returns>
    internal static bool IsTooShort(ContactCollectionRequest request, ContactSummary[] read)
    {
        bool backwards = request.Cursor?.Position == CursorPosition.Before;
        return read.Length == 0 || (backwards && read.Length < request.PageSize.Value);
    }

    /// <summary>
    /// Makes the request for the first page of the same organization's contacts: the same size, and no cursor.
    /// </summary>
    /// <param name="request">The request to start again from.</param>
    /// <returns>The request for the first page.</returns>
    internal static ContactCollectionRequest FirstPage(ContactCollectionRequest request)
    {
        return request with { Cursor = null };
    }

    /// <summary>
    /// Turns the contacts read into a page in name order, with the count and the cursors for the pages either side.
    /// The extra contact read, if any, is dropped; it only showed that another page exists.
    /// </summary>
    /// <param name="request">The request the page answers.</param>
    /// <param name="read">The contacts read for it, in the order they were read.</param>
    /// <param name="total">How many contacts the organization has.</param>
    /// <returns>The page.</returns>
    internal static ContactCollectionResponse CreatePage(
        ContactCollectionRequest request,
        ContactSummary[] read,
        RecordCount total)
    {
        bool backwards = request.Cursor?.Position == CursorPosition.Before;
        bool more = read.Length > request.PageSize.Value;
        ContactSummary[] shown = [.. read.Take(request.PageSize.Value)];
        ContactSummary[] items = backwards ? [.. Enumerable.Reverse(shown)] : shown;

        // Read forwards, the extra contact means a next page, and any cursor means a previous one. Read backwards, it
        // is the other way round.
        bool hasPrevious = backwards ? more : request.Cursor is not null;
        bool hasNext = backwards || more;
        return new ContactCollectionResponse
        {
            Request = request,
            Items = items,
            Previous = hasPrevious ? Edge(items, 0, CursorPosition.Before) : null,
            Next = hasNext ? Edge(items, items.Length - 1, CursorPosition.After) : null,
            Total = total,
        };
    }

    /// <summary>
    /// Makes the cursor for the page beside one of a page's contacts.
    /// </summary>
    /// <param name="items">The page's contacts.</param>
    /// <param name="index">Which contact: the first, for the page before, or the last, for the page after.</param>
    /// <param name="position">Which side of that contact the other page lies on.</param>
    /// <returns>The cursor, or <see langword="null"/> when the page is empty.</returns>
    private static ContactCursor? Edge(ContactSummary[] items, int index, CursorPosition position)
    {
        if (items.Length == 0)
        {
            return null;
        }

        return new ContactCursor(items[index].Id, position);
    }
}
