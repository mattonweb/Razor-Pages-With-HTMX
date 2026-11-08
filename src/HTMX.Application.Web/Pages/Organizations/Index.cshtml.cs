namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The list of organizations: filtered, narrowed by status, sorted and paged, all through the query string, so every
/// view of the list has its own URL and the page works without HTMX.
/// </summary>
public sealed class IndexModel : PageModel
{
    private const string FilterKey = "filter";
    private const string StatusKey = "status";
    private const string SortKey = "sort";
    private const string DirectionKey = "direction";
    private const string PageNumberKey = "pageNumber";
    private const string PageSizeKey = "pageSize";
    private const int DefaultPageSize = 10;
    private const string ListPartial = "_OrganizationList";

    // HTMX sends HX-Request on every request it makes, and HX-History-Restore-Request when the back button needs a
    // page it no longer holds, which must be the whole page.
    private const string HtmxRequestHeader = "HX-Request";
    private const string HtmxHistoryRestoreHeader = "HX-History-Restore-Request";

    private static readonly int[] PageSizes = [DefaultPageSize, 25, 50, PageSize.Largest];

    // What the page shows before OnGetAsync has read the list: nothing, as the defaults would ask for it.
    private static readonly OrganizationCollectionResponse NotYetRead = new OrganizationCollectionResponse
    {
        Request = CreateRequest(new InputModel()),
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
    public InputModel Input { get; set; } = new InputModel();

    /// <summary>
    /// Gets the page of organizations to show, with the request it answers and the counts.
    /// </summary>
    public OrganizationCollectionResponse Organizations { get; private set; } = NotYetRead;

    /// <summary>
    /// Gets the page sizes the Rows per page select offers.
    /// </summary>
    public IReadOnlyList<int> PageSizeOptions { get; } = PageSizes;

    /// <summary>
    /// Gives an industry's name as the page shows it, such as Food and drink.
    /// </summary>
    /// <param name="industry">The industry.</param>
    /// <returns>The display name its <see cref="DisplayAttribute"/> gives.</returns>
    public static string IndustryName(Industry industry)
    {
        string name = industry.ToString();
        FieldInfo? field = typeof(Industry).GetField(name);
        DisplayAttribute? display = field?.GetCustomAttribute<DisplayAttribute>();
        return display?.GetName() ?? name;
    }

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
        IHeaderDictionary responseHeaders = this.Response.Headers;
        responseHeaders.Vary = HtmxRequestHeader;
        if (IsHtmxSwap(this.Request))
        {
            return this.Partial(ListPartial, this);
        }

        return this.Page();
    }

    /// <summary>
    /// Builds the query string for a link to another view of the list: this one, with the changes given. A link that
    /// changes the status starts again at the first page; only the pager passes a page.
    /// </summary>
    /// <param name="status">The status to show instead, if any.</param>
    /// <param name="page">The page to show instead, if any.</param>
    /// <param name="withoutFilter">Whether the link clears the filter.</param>
    /// <returns>The query string values, leaving out every value that is the default.</returns>
    public Dictionary<string, string> RouteValues(
        RecordStatus? status = null,
        PageNumber? page = null,
        bool withoutFilter = false)
    {
        OrganizationCollectionRequest current = this.Organizations.Request;
        Dictionary<string, string> values = [];
        AddUnlessDefault(values, FilterKey, withoutFilter ? null : current.Filter?.Value, null);
        AddUnlessDefault(values, StatusKey, Name(status ?? current.Status), Name(RecordStatus.Active));
        AddUnlessDefault(values, SortKey, Name(current.Sort), Name(OrganizationSortColumn.Name));
        AddUnlessDefault(values, DirectionKey, Name(current.Direction), Name(SortDirection.Ascending));
        AddUnlessDefault(values, PageNumberKey, page?.Value.ToString(CultureInfo.InvariantCulture), "1");
        AddUnlessDefault(values, PageSizeKey, Number(current.PageSize.Value), Number(DefaultPageSize));
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
        SortDirection direction = NaturalDirection(column);
        if (column == current.Sort)
        {
            direction = current.Direction == SortDirection.Ascending
                ? SortDirection.Descending
                : SortDirection.Ascending;
        }

        Dictionary<string, string> values = this.RouteValues();
        values[SortKey] = Name(column);
        values[DirectionKey] = Name(direction);
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
        return column == current.Sort ? Name(current.Direction) : null;
    }

    /// <summary>
    /// Says whether a request came from HTMX to swap part of a page, rather than to load a whole one.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns><see langword="true"/> when HTMX sent it and is not restoring a page from its history.</returns>
    private static bool IsHtmxSwap(HttpRequest request)
    {
        IHeaderDictionary headers = request.Headers;
        return headers[HtmxRequestHeader] == "true" && headers[HtmxHistoryRestoreHeader] != "true";
    }

    /// <summary>
    /// Turns the query string into a request, falling back to the default for anything missing or not recognized.
    /// </summary>
    /// <param name="input">The query string, as bound.</param>
    /// <returns>The request.</returns>
    private static OrganizationCollectionRequest CreateRequest(InputModel input)
    {
        return new OrganizationCollectionRequest
        {
            Filter = string.IsNullOrWhiteSpace(input.Filter) ? null : new FilterText(input.Filter),
            Status = ParseOrDefault(input.Status, RecordStatus.Active),
            Sort = ParseOrDefault(input.Sort, OrganizationSortColumn.Name),
            Direction = ParseOrDefault(input.Direction, SortDirection.Ascending),
            Page = new PageNumber(Math.Max(1, input.PageNumber ?? 1)),
            PageSize = ChoosePageSize(input.PageSize),
        };
    }

    /// <summary>
    /// Reads a choice from the query string, ignoring case, or gives the default when it is missing or unknown.
    /// </summary>
    /// <typeparam name="TChoice">The list of choices.</typeparam>
    /// <param name="text">The value as sent.</param>
    /// <param name="fallback">The default.</param>
    /// <returns>The choice.</returns>
    private static TChoice ParseOrDefault<TChoice>(string? text, TChoice fallback)
        where TChoice : struct, Enum
    {
        return Enum.TryParse(text, ignoreCase: true, out TChoice parsed) && Enum.IsDefined(parsed) ? parsed : fallback;
    }

    /// <summary>
    /// Reads the page size from the query string, or gives the default when it is missing or not one the select offers.
    /// </summary>
    /// <param name="size">The page size as sent, if any.</param>
    /// <returns>The page size.</returns>
    private static PageSize ChoosePageSize(int? size)
    {
        if (size is int offered && Array.IndexOf(PageSizes, offered) >= 0)
        {
            return new PageSize(offered);
        }

        return new PageSize(DefaultPageSize);
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
    /// Adds a value to a query string unless it is missing or the default, so the default view's URL stays plain.
    /// </summary>
    /// <param name="values">The query string being built.</param>
    /// <param name="key">The value's name.</param>
    /// <param name="value">The value, if any.</param>
    /// <param name="defaultValue">The value that is left out.</param>
    private static void AddUnlessDefault(
        Dictionary<string, string> values,
        string key,
        string? value,
        string? defaultValue)
    {
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
    private static string Name(Enum choice)
    {
        string name = choice.ToString();
        return name.ToLowerInvariant();
    }

    /// <summary>
    /// Writes a number as it appears in a URL, the same whatever the browser's language.
    /// </summary>
    /// <param name="number">The number.</param>
    /// <returns>The number in the invariant culture.</returns>
    private static string Number(int number)
    {
        return number.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The list's query string, as the browser sent it. Every value is optional, and is kept as sent until the page
    /// turns it into a request.
    /// </summary>
    public sealed class InputModel
    {
        /// <summary>
        /// Gets or sets the text the names must contain.
        /// </summary>
        public string? Filter { get; set; }

        /// <summary>
        /// Gets or sets which organizations to show: active, archived or all.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets what to sort by: name, industry, contacts or updated.
        /// </summary>
        public string? Sort { get; set; }

        /// <summary>
        /// Gets or sets which way to sort: ascending or descending.
        /// </summary>
        public string? Direction { get; set; }

        /// <summary>
        /// Gets or sets which page to show, from 1.
        /// </summary>
        public int? PageNumber { get; set; }

        /// <summary>
        /// Gets or sets how many organizations a page holds: 10, 25, 50 or 100.
        /// </summary>
        public int? PageSize { get; set; }
    }
}
