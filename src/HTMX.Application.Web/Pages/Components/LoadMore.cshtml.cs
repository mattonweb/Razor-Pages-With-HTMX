namespace HTMX.Application.Web.Pages.Components;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Load more page, at /components/load-more: the active organizations by name, ten at first, and a Load more
/// button that adds the next ten under them, up to a hundred.
/// </summary>
/// <remarks>
/// The address holds how many rows the page shows, <c>?shown=20</c>, which loads the page with that many on its own.
/// Without HTMX, Load more is a link to the next such address. With HTMX it asks for the ten after the last row's
/// organization, <c>?shown=20&amp;after=10216</c>, by key as the lists page; the answer adds them to the table, draws
/// the footer again with the new count and the next link, and names the new address in <c>HX-Replace-Url</c>, so Back
/// steps over the loads rather than through each one. The first new row's link takes the keyboard.
/// </remarks>
public sealed class LoadMoreModel : PageModel
{
    /// <summary>
    /// How many rows the page shows first, and how many each Load more adds.
    /// </summary>
    public const int Step = 10;

    // The answer to HTMX's Load more: the new rows, and the footer for the layout of the card.
    private const string MorePartial = "_LoadMoreAnswer";

    private readonly IOrganizationService _organizations;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadMoreModel"/> class.
    /// </summary>
    /// <param name="organizations">Reads the organizations.</param>
    public LoadMoreModel(IOrganizationService organizations)
    {
        ArgumentNullException.ThrowIfNull(organizations);
        this._organizations = organizations;
    }

    /// <summary>
    /// Gets the rows this answer draws: every row the page shows, or with HTMX only the ones Load more adds.
    /// </summary>
    public OrganizationSummary[] Rows { get; private set; } = [];

    /// <summary>
    /// Gets the position, counting from 1, of the first row this answer draws.
    /// </summary>
    public int FirstPosition { get; private set; } = 1;

    /// <summary>
    /// Gets how many rows the page shows once this answer is drawn.
    /// </summary>
    public int Shown { get; private set; }

    /// <summary>
    /// Gets how many active organizations there are.
    /// </summary>
    public int Matching { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there are organizations after the last row.
    /// </summary>
    public bool More { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the first row this answer draws takes the keyboard: the first row Load more
    /// added, with HTMX.
    /// </summary>
    public bool FirstRowFocused { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Gets the identifier of the last row's organization, which the next Load more asks for the rows after.
    /// </summary>
    public long? Last { get; private set; }

    /// <summary>
    /// Shows the first rows, as many as the address asks, or, for HTMX's Load more, the next ten after the
    /// organization it names, with the footer and the page's new address.
    /// </summary>
    /// <param name="shown">How many rows the page shows, from the query string: ten at first, at most a
    /// hundred.</param>
    /// <param name="after">The last row's organization, which HTMX's Load more names; ignored without HTMX.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The rows and the footer for HTMX, or the whole page.</returns>
    public async Task<IActionResult> OnGetAsync(int? shown, long? after, CancellationToken cancellationToken)
    {
        int asked = Math.Clamp(shown ?? Step, Step, PageSize.Largest);
        if (after is long last && HtmxHeaders.IsPartialRequest(this.Request))
        {
            return await this.MoreAsync(asked - Step, last, cancellationToken).ConfigureAwait(false);
        }

        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        await this.ReadAsync(cursor: null, asked, cancellationToken).ConfigureAwait(false);
        this.Shown = this.Rows.Length;
        return this.Page();
    }

    /// <summary>
    /// Answers HTMX's Load more: the ten rows after the last one shown, which take the keyboard at the first, the
    /// footer drawn again, and the page's new address.
    /// </summary>
    /// <param name="before">How many rows the page showed before this answer.</param>
    /// <param name="last">The last row's organization.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The rows and the footer.</returns>
    private async Task<IActionResult> MoreAsync(int before, long last, CancellationToken cancellationToken)
    {
        OrganizationCursor cursor = new OrganizationCursor(new OrganizationId(last), CursorPosition.After);
        await this.ReadAsync(cursor, Step, cancellationToken).ConfigureAwait(false);
        this.FirstPosition = before + 1;
        this.Shown = before + this.Rows.Length;
        this.FirstRowFocused = true;
        string address = this.Url.Page("./LoadMore", new { shown = this.Shown })
            ?? throw new InvalidOperationException("The Load more page has no address.");
        HtmxHeaders.ReplaceAddress(this.Response, address);
        return this.Partial(MorePartial, this);
    }

    /// <summary>
    /// Reads a page of the active organizations by name.
    /// </summary>
    /// <param name="cursor">Where the page starts, or <see langword="null"/> for the first.</param>
    /// <param name="size">How many rows the page holds.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>A task that completes when the rows are read.</returns>
    private async Task ReadAsync(OrganizationCursor? cursor, int size, CancellationToken cancellationToken)
    {
        OrganizationCollectionRequest request = new OrganizationCollectionRequest
        {
            Status = RecordStatus.Active,
            Sort = OrganizationSortColumn.Name,
            Direction = SortDirection.Ascending,
            Cursor = cursor,
            PageSize = new PageSize(size),
        };
        OrganizationCollectionResponse response = await this._organizations
            .ListAsync(request, cancellationToken)
            .ConfigureAwait(false);
        this.Rows = response.Items;
        this.Matching = response.Matching.Value;
        this.More = response.Next is not null;
        this.Last = this.Rows.Length == 0 ? null : this.Rows[this.Rows.Length - 1].Id.Value;
    }
}
