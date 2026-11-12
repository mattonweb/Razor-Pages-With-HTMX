namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The list of organizations: filtered, narrowed by status, sorted and paged, all through the query string, so every
/// view of the list has its own URL and the page works without HTMX. Paging is key-based: Next and Previous name the
/// organization the next page starts after, or the previous page ends before, never a page number. The query string's
/// names, and how its values are read and written, are <see cref="ListQuery"/>'s, which every list shares.
/// </summary>
/// <remarks>
/// Each row's menu archives or restores its organization at once and says so in a toast whose Undo does the opposite.
/// The row's form posts to the list's own address with the handler and the organization added, so the POST binds where
/// the list is just as the GET does, and reads the same view again. In the Active view, the default, an archive takes
/// its row out of the list, and the counts on Active and Archived change with it, so with HTMX the answer is the list's
/// whole card; without HTMX, a redirect back to the same view.
/// </remarks>
public sealed class IndexModel : PageModel
{
    private const string ListPage = "./Index";
    private const string ListPartial = "_OrganizationList";
    private const string ListChangedPartial = "_OrganizationListChanged";
    private const string ArchiveHandler = "Archive";
    private const string RestoreHandler = "Restore";

    // What an action on a row, and its Undo, replaces: the list's card, which holds the counts an action changes.
    private const string ListTarget = "#organization-list";

    // The query string name of the organization a row's action is for; the handlers' parameter has the same name.
    private const string OrganizationKey = "organization";

    // What the page shows before OnGetAsync has read the list: nothing, as the defaults would ask for it.
    private static readonly OrganizationCollectionResponse NotYetRead = new OrganizationCollectionResponse
    {
        Request = CreateRequest(new ListInput()),
        Items = [],
        Matching = new RecordCount(0),
        Active = new RecordCount(0),
        Archived = new RecordCount(0),
        All = new RecordCount(0),
    };

    private readonly IOrganizationService _organizations;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="organizations">Reads the organizations.</param>
    public IndexModel(IOrganizationService organizations)
    {
        ArgumentNullException.ThrowIfNull(organizations);
        this._organizations = organizations;
    }

    /// <summary>
    /// Gets or sets the list's query string, as the browser sent it. Model binding writes it; the page only reads it.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public ListInput Input { get; set; } = new ListInput();

    /// <summary>
    /// Gets the page of organizations to show, with the request it answers and the counts.
    /// </summary>
    public OrganizationCollectionResponse Organizations { get; private set; } = NotYetRead;

    /// <summary>
    /// Gets the toast that says what an action on a row just did, or the one another page left for this one, such as
    /// a deleted organization's; <see langword="null"/> when there is neither.
    /// </summary>
    public Toast? Toast { get; private set; }

    /// <summary>
    /// Gets the organization whose row's menu button takes the focus when an action's answer draws the list again: the
    /// row the action was for, wherever it now is on the page, or, once the action has taken that row off the page,
    /// the row now in its place. It is <see langword="null"/> when no action has run, when the action left no row, and
    /// when an Undo sent from the toast was for a row that is not on the page.
    /// </summary>
    public OrganizationId? Focused { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the empty list's heading takes the focus, as it does when an action takes the
    /// last row out of the list.
    /// </summary>
    public bool EmptyFocused { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX asked for the whole page, as
    /// Previous, Next and Rows per page do to take its main content, rather than the browser loading it on its own.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Gives the part of a website's address a list shows under the name, such as placeholder.example.
    /// </summary>
    /// <param name="website">The website.</param>
    /// <returns>The address's host.</returns>
    public static string WebsiteHost(WebAddress website)
    {
        Uri address = new Uri(website.Value);
        return address.Host;
    }

    /// <summary>
    /// Reads the page of organizations the query string asks for. A value the page does not recognize falls back to
    /// its default, so a mistyped URL still shows a list.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>Only the list's card when HTMX asked for it, to swap into the page it already shows; the whole page
    /// otherwise.</returns>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        OrganizationCollectionRequest request = CreateRequest(this.Input);
        this.Organizations = await this._organizations.ListAsync(request, cancellationToken).ConfigureAwait(false);

        // One URL answers with two different bodies, so a cache must keep them apart by the header that chooses.
        HtmxHeaders.VaryByRequestType(this.Response);
        if (HtmxHeaders.IsPartialRequest(this.Request))
        {
            return this.Partial(ListPartial, this);
        }

        // A toast waits for the whole page, which a deleted organization's page loads, and is not spent on a swap of
        // the card.
        this.Toast = TempDataToasts.Take(this.TempData, ListTarget);
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        return this.Page();
    }

    /// <summary>
    /// Archives the organization on one row, and says so in a toast whose Undo restores it. In the Active view the row
    /// leaves the list.
    /// </summary>
    /// <param name="organization">The organization's identifier, from the row's address.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The list's card and the toast for HTMX; a redirect back to the same view of the list otherwise; 400
    /// for an identifier that cannot be one.</returns>
    public Task<IActionResult> OnPostArchiveAsync(long organization, CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(organization, ChangeKind.Archived, cancellationToken);
    }

    /// <summary>
    /// Restores the archived organization on one row, and says so in a toast whose Undo archives it again. In the
    /// Archived view the row leaves the list.
    /// </summary>
    /// <param name="organization">The organization's identifier, from the row's address.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The list's card and the toast for HTMX; a redirect back to the same view of the list otherwise; 400
    /// for an identifier that cannot be one.</returns>
    public Task<IActionResult> OnPostRestoreAsync(long organization, CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(organization, ChangeKind.Restored, cancellationToken);
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
        OrganizationCursor? cursor = null,
        bool withoutFilter = false)
    {
        OrganizationCollectionRequest current = this.Organizations.Request;
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
            ListQuery.Name(OrganizationSortColumn.Name));
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
    /// column's link sorts by it in its natural order: names and industries A to Z, contacts and changes most first.
    /// </summary>
    /// <param name="column">The header's column.</param>
    /// <returns>The query string values, starting again at the first page.</returns>
    public Dictionary<string, string> SortRouteValues(OrganizationSortColumn column)
    {
        OrganizationCollectionRequest current = this.Organizations.Request;
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
    public string? AriaSort(OrganizationSortColumn column)
    {
        OrganizationCollectionRequest current = this.Organizations.Request;
        return ListQuery.AriaSort(column, current.Sort, current.Direction);
    }

    /// <summary>
    /// Writes the address a row's menu posts to: Archive for an active organization, Restore for an archived one, with
    /// where the list is.
    /// </summary>
    /// <param name="organization">The row's organization.</param>
    /// <returns>The address.</returns>
    public string RowAction(OrganizationSummary organization)
    {
        ArgumentNullException.ThrowIfNull(organization);
        string handler = organization.ArchivedOn is null ? ArchiveHandler : RestoreHandler;
        return this.RowAddress(handler, organization.Id);
    }

    /// <summary>
    /// Turns the query string into a request, falling back to the default for anything missing or not recognized.
    /// </summary>
    /// <param name="input">The query string, as bound.</param>
    /// <returns>The request.</returns>
    private static OrganizationCollectionRequest CreateRequest(ListInput input)
    {
        return new OrganizationCollectionRequest
        {
            Filter = string.IsNullOrWhiteSpace(input.Filter) ? null : new FilterText(input.Filter),
            Status = ListQuery.ParseOrDefault(input.Status, RecordStatus.Active),
            Sort = ListQuery.ParseOrDefault(input.Sort, OrganizationSortColumn.Name),
            Direction = ListQuery.ParseOrDefault(input.Direction, SortDirection.Ascending),
            Cursor = CreateCursor(input),
            PageSize = ListQuery.ChoosePageSize(input.PageSize),
        };
    }

    /// <summary>
    /// Reads where the page lies from the query string: after one organization, before one, or, with neither, at the
    /// start. An identifier that names no organization is the service's to deal with; it shows the first page.
    /// </summary>
    /// <param name="input">The query string, as bound.</param>
    /// <returns>The cursor, or <see langword="null"/> for the first page.</returns>
    private static OrganizationCursor? CreateCursor(ListInput input)
    {
        if (input.After is long after)
        {
            return new OrganizationCursor(new OrganizationId(after), CursorPosition.After);
        }

        if (input.Before is long before)
        {
            return new OrganizationCursor(new OrganizationId(before), CursorPosition.Before);
        }

        return null;
    }

    /// <summary>
    /// Writes a cursor's organization as it appears in a URL, when the cursor lies on the side given.
    /// </summary>
    /// <param name="cursor">The cursor, if any.</param>
    /// <param name="position">The side the value is for: after or before.</param>
    /// <returns>The identifier, or <see langword="null"/> when there is no cursor on that side.</returns>
    private static string? CursorId(OrganizationCursor? cursor, CursorPosition position)
    {
        if (cursor is not OrganizationCursor named || named.Position != position)
        {
            return null;
        }

        return ListQuery.Number(named.Id.Value);
    }

    /// <summary>
    /// Says which way a column sorts when it is first chosen.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <returns>Ascending for names and industries, descending for counts and dates.</returns>
    private static SortDirection NaturalDirection(OrganizationSortColumn column)
    {
        return column is OrganizationSortColumn.Contacts or OrganizationSortColumn.Updated
            ? SortDirection.Descending
            : SortDirection.Ascending;
    }

    /// <summary>
    /// Archives or restores an organization, reads the same view of the list again, and answers with it and a toast
    /// that says what happened.
    /// </summary>
    /// <param name="organization">The organization's identifier, as sent.</param>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The list's card and the toast for HTMX; a redirect back to the same view of the list
    /// otherwise.</returns>
    private async Task<IActionResult> ChangeStatusAsync(
        long organization,
        ChangeKind change,
        CancellationToken cancellationToken)
    {
        if (organization < 1)
        {
            return this.BadRequest();
        }

        // Read the page as it is shown first: once the action has taken the row out of the list, the page read again
        // no longer says where the row was, and the keyboard goes back to that place.
        OrganizationCollectionRequest request = CreateRequest(this.Input);
        OrganizationCollectionResponse shown = await this._organizations
            .ListAsync(request, cancellationToken)
            .ConfigureAwait(false);
        OrganizationStatusRequest status = new OrganizationStatusRequest
        {
            Id = new OrganizationId(organization),
            ChangedBy = SignedInUser.Of(this.User),
        };
        UpdateOutcome outcome = change == ChangeKind.Archived
            ? await this._organizations.ArchiveAsync(status, cancellationToken).ConfigureAwait(false)
            : await this._organizations.RestoreAsync(status, cancellationToken).ConfigureAwait(false);

        // The toast names the organization from its own record, because an Undo sent from the toast can be for a row
        // that neither page shows.
        OrganizationSingleResponse? found = await this._organizations
            .FindAsync(status.Id, cancellationToken)
            .ConfigureAwait(false);
        this.Organizations = await this._organizations.ListAsync(request, cancellationToken).ConfigureAwait(false);
        this.FocusAfterAction(status.Id, Array.FindIndex(shown.Items, row => row.Id == status.Id));

        // Undo is offered only for a change that was made: it posts the opposite change, from the same view.
        string? undo = null;
        if (outcome == UpdateOutcome.Saved)
        {
            string opposite = change == ChangeKind.Archived ? RestoreHandler : ArchiveHandler;
            undo = this.RowAddress(opposite, status.Id);
        }

        string name = found?.Name.Value ?? "The organization";
        return this.Answer(OrganizationMessages.Status(change, outcome, name), undo);
    }

    /// <summary>
    /// Chooses where the keyboard goes when an action's answer draws the list again. The row's own menu button, when
    /// the row is on the page read again, wherever it now is. When the action took the row off the page, the menu
    /// button of the row now in its place, or of the last row when the page is now shorter, or the empty list's
    /// heading when no row is left. Nowhere new when the row was not on the page shown and is not on it now, as for an
    /// Undo sent from the toast after the list has moved on: HTMX puts the keyboard back on the new toast's Undo.
    /// </summary>
    /// <param name="id">The organization the action was for.</param>
    /// <param name="place">Where its row was on the page shown, counting from 0, or -1 when it was not there.</param>
    private void FocusAfterAction(OrganizationId id, int place)
    {
        OrganizationSummary[] rows = this.Organizations.Items;
        if (Array.Exists(rows, row => row.Id == id))
        {
            this.Focused = id;
            return;
        }

        if (place < 0)
        {
            return;
        }

        if (rows.Length == 0)
        {
            this.EmptyFocused = true;
            return;
        }

        this.Focused = rows[Math.Min(place, rows.Length - 1)].Id;
    }

    /// <summary>
    /// Gives the query string values that bring back the view of the list the page shows: the filter, status, sort
    /// and size, and where the page lies. The cursor is the one the service answered with, which names the first page
    /// when the organization it named has gone.
    /// </summary>
    /// <returns>The values, leaving out every value that is the default.</returns>
    private Dictionary<string, string> ListValues()
    {
        OrganizationCollectionRequest current = this.Organizations.Request;
        return this.RouteValues(cursor: current.Cursor);
    }

    /// <summary>
    /// Writes the address of one of the row handlers for an organization, with where the list is.
    /// </summary>
    /// <param name="handler">The handler, such as <c>Archive</c>.</param>
    /// <param name="organization">The organization.</param>
    /// <returns>The address.</returns>
    private string RowAddress(string handler, OrganizationId organization)
    {
        Dictionary<string, string> values = this.ListValues();
        values[OrganizationKey] = ListQuery.Number(organization.Value);
        return this.Url.Page(ListPage, handler, values)
            ?? throw new InvalidOperationException($"The handler {handler} has no address.");
    }

    /// <summary>
    /// Answers an action on a row. HTMX gets the list's card and the toast, to swap in place, and the list's address
    /// to put in the address bar: the toast's Undo can be sent after the list has moved to another page or filter, and
    /// its answer draws the list where the action ran. A browser without HTMX is redirected back to the same view of
    /// the list, so that refreshing it does not post again, and the toast waits in TempData for the list to show.
    /// </summary>
    /// <param name="message">The toast's message.</param>
    /// <param name="undoAction">The address Undo posts to, or <see langword="null"/> for no Undo.</param>
    /// <returns>The card and the toast, or the redirect.</returns>
    private IActionResult Answer(string message, string? undoAction)
    {
        Toast toast = new Toast
        {
            Message = message,
            UndoAction = undoAction,
            UndoTarget = undoAction is null ? null : ListTarget,
        };
        Dictionary<string, string> values = this.ListValues();
        if (!HtmxHeaders.IsPartialRequest(this.Request))
        {
            TempDataToasts.Leave(this.TempData, toast);
            return this.RedirectToPage(ListPage, values);
        }

        string address = this.Url.Page(ListPage, values)
            ?? throw new InvalidOperationException("The list has no address.");
        HtmxHeaders.ReplaceAddress(this.Response, address);
        this.Toast = toast;
        return this.Partial(ListChangedPartial, this);
    }
}
