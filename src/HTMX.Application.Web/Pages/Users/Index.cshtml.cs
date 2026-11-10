namespace HTMX.Application.Web.Pages.Users;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The list of users, the people who use the application and manage its records: filtered by name, username or email
/// address, narrowed by status, sorted and paged, all through the query string, as the organizations list is. Every
/// view of the list has its own URL, the page works without HTMX, and paging is key-based. The query string's names,
/// and how its values are read and written, are <see cref="ListQuery"/>'s, which every list shares.
/// </summary>
public sealed class IndexModel : PageModel
{
    private const string ListPartial = "_UserList";

    // What the page shows before OnGetAsync has read the list: nothing, as the defaults would ask for it.
    private static readonly UserCollectionResponse NotYetRead = new UserCollectionResponse
    {
        Request = CreateRequest(new ListInput()),
        Items = [],
        Matching = new RecordCount(0),
        Active = new RecordCount(0),
        Archived = new RecordCount(0),
        All = new RecordCount(0),
    };

    private readonly IUserService _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="users">Reads the users.</param>
    public IndexModel(IUserService users)
    {
        ArgumentNullException.ThrowIfNull(users);
        this._users = users;
    }

    /// <summary>
    /// Gets or sets the list's query string, as the browser sent it. Model binding writes it; the page only reads it.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public ListInput Input { get; set; } = new ListInput();

    /// <summary>
    /// Gets the page of users to show, with the request it answers and the counts.
    /// </summary>
    public UserCollectionResponse Users { get; private set; } = NotYetRead;

    /// <summary>
    /// Gets the toast another page left for this one, such as a deleted user's, or <see langword="null"/> when none
    /// was left.
    /// </summary>
    public Toast? Toast { get; private set; }

    /// <summary>
    /// Reads the page of users the query string asks for. A value the page does not recognize falls back to its
    /// default, so a mistyped URL still shows a list.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>Only the list's card when HTMX asked for it, to swap into the page it already shows; the whole page
    /// otherwise.</returns>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        UserCollectionRequest request = CreateRequest(this.Input);
        this.Users = await this._users.ListAsync(request, cancellationToken).ConfigureAwait(false);

        // One URL answers with two different bodies, so a cache must keep them apart by the header that chooses.
        HtmxHeaders.VaryByRequestType(this.Response);
        if (HtmxHeaders.IsPartialRequest(this.Request))
        {
            return this.Partial(ListPartial, this);
        }

        // A toast waits for the whole page, which a deleted user's page loads, and is not spent on a swap of the card.
        this.Toast = TempDataToasts.Take(this.TempData, "#user-list");
        return this.Page();
    }

    /// <summary>
    /// Builds the query string for a link to another view of the list: this one, with the changes given. A link that
    /// changes the status starts again at the first page; only the pager passes a cursor.
    /// </summary>
    /// <param name="status">The status to show instead, if any.</param>
    /// <param name="cursor">Where the page to show lies, for the pager's Previous and Next.</param>
    /// <param name="withoutFilter">Whether the link clears the filter.</param>
    /// <returns>The query string values, leaving out every value that is the default.</returns>
    public Dictionary<string, string> RouteValues(
        RecordStatus? status = null,
        UserCursor? cursor = null,
        bool withoutFilter = false)
    {
        UserCollectionRequest current = this.Users.Request;
        string? filter = withoutFilter ? null : current.Filter?.Value;
        Dictionary<string, string> values = [];
        ListQuery.AddUnlessDefault(values, ListQuery.FilterKey, filter, null);
        ListQuery.AddUnlessDefault(
            values,
            ListQuery.StatusKey,
            ListQuery.Name(status ?? current.Status),
            ListQuery.Name(RecordStatus.Active));
        ListQuery.AddUnlessDefault(
            values,
            ListQuery.SortKey,
            ListQuery.Name(current.Sort),
            ListQuery.Name(UserSortColumn.Name));
        ListQuery.AddUnlessDefault(
            values,
            ListQuery.DirectionKey,
            ListQuery.Name(current.Direction),
            ListQuery.Name(SortDirection.Ascending));
        ListQuery.AddUnlessDefault(values, ListQuery.AfterKey, CursorId(cursor, CursorPosition.After), null);
        ListQuery.AddUnlessDefault(values, ListQuery.BeforeKey, CursorId(cursor, CursorPosition.Before), null);
        ListQuery.AddUnlessDefault(
            values,
            ListQuery.PageSizeKey,
            ListQuery.Number(current.PageSize.Value),
            ListQuery.Number(ListQuery.DefaultPageSize));
        return values;
    }

    /// <summary>
    /// Builds the query string for a sort header's link. The sorted column's link turns the order round; another
    /// column's link sorts by it in its natural order: names, usernames and email addresses A to Z, changes most recent
    /// first.
    /// </summary>
    /// <param name="column">The header's column.</param>
    /// <returns>The query string values, starting again at the first page.</returns>
    public Dictionary<string, string> SortRouteValues(UserSortColumn column)
    {
        UserCollectionRequest current = this.Users.Request;
        SortDirection direction = ListQuery.NextDirection(
            column,
            current.Sort,
            current.Direction,
            NaturalDirection(column));
        Dictionary<string, string> values = this.RouteValues();
        values[ListQuery.SortKey] = ListQuery.Name(column);
        values[ListQuery.DirectionKey] = ListQuery.Name(direction);
        return values;
    }

    /// <summary>
    /// Says how a column's header is sorted, for its <c>aria-sort</c> attribute.
    /// </summary>
    /// <param name="column">The header's column.</param>
    /// <returns><c>ascending</c> or <c>descending</c> for the sorted column, and <see langword="null"/> for the
    /// others, which leaves the attribute out.</returns>
    public string? AriaSort(UserSortColumn column)
    {
        UserCollectionRequest current = this.Users.Request;
        return ListQuery.AriaSort(column, current.Sort, current.Direction);
    }

    /// <summary>
    /// Turns the query string into a request, falling back to the default for anything missing or not recognized.
    /// </summary>
    /// <param name="input">The query string, as bound.</param>
    /// <returns>The request.</returns>
    private static UserCollectionRequest CreateRequest(ListInput input)
    {
        return new UserCollectionRequest
        {
            Filter = string.IsNullOrWhiteSpace(input.Filter) ? null : new FilterText(input.Filter),
            Status = ListQuery.ParseOrDefault(input.Status, RecordStatus.Active),
            Sort = ListQuery.ParseOrDefault(input.Sort, UserSortColumn.Name),
            Direction = ListQuery.ParseOrDefault(input.Direction, SortDirection.Ascending),
            Cursor = CreateCursor(input),
            PageSize = ListQuery.ChoosePageSize(input.PageSize),
        };
    }

    /// <summary>
    /// Reads where the page lies from the query string: after one user, before one, or, with neither, at the start. An
    /// identifier that names no user is the service's to deal with; it shows the first page.
    /// </summary>
    /// <param name="input">The query string, as bound.</param>
    /// <returns>The cursor, or <see langword="null"/> for the first page.</returns>
    private static UserCursor? CreateCursor(ListInput input)
    {
        if (input.After is long after)
        {
            return new UserCursor(new UserId(after), CursorPosition.After);
        }

        if (input.Before is long before)
        {
            return new UserCursor(new UserId(before), CursorPosition.Before);
        }

        return null;
    }

    /// <summary>
    /// Writes a cursor's user as they appear in a URL, when the cursor lies on the side given.
    /// </summary>
    /// <param name="cursor">The cursor, if any.</param>
    /// <param name="position">The side the value is for: after or before.</param>
    /// <returns>The identifier, or <see langword="null"/> when there is no cursor on that side.</returns>
    private static string? CursorId(UserCursor? cursor, CursorPosition position)
    {
        if (cursor is not UserCursor named || named.Position != position)
        {
            return null;
        }

        return ListQuery.Number(named.Id.Value);
    }

    /// <summary>
    /// Says which way a column sorts when it is first chosen.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <returns>Descending for when a user was last changed, ascending for the others.</returns>
    private static SortDirection NaturalDirection(UserSortColumn column)
    {
        return column == UserSortColumn.Updated ? SortDirection.Descending : SortDirection.Ascending;
    }
}
