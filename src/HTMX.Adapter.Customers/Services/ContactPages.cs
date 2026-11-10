namespace HTMX.Adapter.Customers.Services;

using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// An organization's list of contacts' side of key-based paging: which request reads the first page, and the response
/// a page of contacts becomes. The rules every list shares are <see cref="ListPages"/>'s; <see cref="ContactService"/>
/// reads the contacts and hands them here.
/// </summary>
internal static class ContactPages
{
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
        ListPage<ContactSummary, ContactCursor> page = ListPages.Cut(
            read,
            request.Cursor?.Position,
            request.PageSize,
            static (contact, side) => new ContactCursor(contact.Id, side));
        return new ContactCollectionResponse
        {
            Request = request,
            Items = page.Items,
            Previous = page.Previous,
            Next = page.Next,
            Total = total,
        };
    }
}
