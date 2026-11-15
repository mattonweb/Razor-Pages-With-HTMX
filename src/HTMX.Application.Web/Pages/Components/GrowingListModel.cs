namespace HTMX.Application.Web.Pages.Components;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// What a list that grows ten rows at a time shares: the active organizations by name, ten at first and up to a
/// hundred, with every row it has shown kept. The Load more page grows when Load more is pressed; the Load on scroll
/// page also grows as its last row comes into view.
/// </summary>
/// <remarks>
/// The address holds how many rows the page shows, <c>?shown=20</c>, which loads the page with that many on its own.
/// Without HTMX, Load more is a link to the next such address. With HTMX a load asks for the ten after the last row's
/// organization, <c>?shown=20&amp;after=10216</c>, by key as the lists page; the answer adds them to the table, draws
/// again what counts them, and names the new address in <c>HX-Replace-Url</c>, so Back steps over the loads rather
/// than through each one. Razor Pages finds the handler here for each page that inherits it.
/// </remarks>
public abstract class GrowingListModel : PageModel
{
    /// <summary>
    /// How many rows the page shows first, and how many each load adds.
    /// </summary>
    public const int Step = 10;

    private readonly IOrganizationService _organizations;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrowingListModel"/> class.
    /// </summary>
    /// <param name="organizations">Reads the organizations.</param>
    protected GrowingListModel(IOrganizationService organizations)
    {
        ArgumentNullException.ThrowIfNull(organizations);
        this._organizations = organizations;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the load was one the last row sent as it came into view, which the user
    /// did not ask for, so that its rows take no keyboard. Its request names it in the query string.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool Scrolled { get; set; }

    /// <summary>
    /// Gets the rows this answer draws: every row the page shows, or with HTMX only the ones a load adds.
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
    /// Gets a value indicating whether the first row this answer draws takes the keyboard: the first row a load the
    /// user asked for added, with HTMX.
    /// </summary>
    public bool FirstRowFocused { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Gets the identifier of the last row's organization, which the next load asks for the rows after.
    /// </summary>
    public long? Last { get; private set; }

    /// <summary>
    /// Gets the address the last row asks for the next rows at as it comes into view, or <see langword="null"/>: on a
    /// page that grows only when asked, or when the list may grow no more.
    /// </summary>
    public string? GrowsFrom { get; private set; }

    /// <summary>
    /// Gets the page's name, such as <c>./LoadMore</c>, for the addresses its answers name.
    /// </summary>
    protected abstract string PageName { get; }

    /// <summary>
    /// Gets the partial that answers a load: the new rows, and whatever the page draws again with them.
    /// </summary>
    protected abstract string AnswerPartial { get; }

    /// <summary>
    /// Gets a value indicating whether the page also grows as its last row comes into view.
    /// </summary>
    protected abstract bool GrowsOnScroll { get; }

    /// <summary>
    /// Shows the first rows, as many as the address asks, or, for a load HTMX sends, the next ten after the
    /// organization it names, with what counts them and the page's new address.
    /// </summary>
    /// <param name="shown">How many rows the page shows, from the query string: ten at first, at most a
    /// hundred.</param>
    /// <param name="after">The last row's organization, which a load HTMX sends names; ignored without HTMX.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The rows and what counts them for HTMX, or the whole page.</returns>
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
        this.GrowsFrom = this.NextGrowth();
        return this.Page();
    }

    /// <summary>
    /// Answers a load HTMX sends: the ten rows after the last one shown, which take the keyboard at the first when the
    /// user asked for them, what counts them drawn again, and the page's new address.
    /// </summary>
    /// <param name="before">How many rows the page showed before this answer.</param>
    /// <param name="last">The last row's organization.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The rows and what counts them.</returns>
    private async Task<IActionResult> MoreAsync(int before, long last, CancellationToken cancellationToken)
    {
        OrganizationCursor cursor = new OrganizationCursor(new OrganizationId(last), CursorPosition.After);
        await this.ReadAsync(cursor, Step, cancellationToken).ConfigureAwait(false);
        this.FirstPosition = before + 1;
        this.Shown = before + this.Rows.Length;
        this.FirstRowFocused = !this.Scrolled;
        this.GrowsFrom = this.NextGrowth();
        string address = this.Url.Page(this.PageName, new { shown = this.Shown })
            ?? throw new InvalidOperationException($"The page {this.PageName} has no address.");
        HtmxHeaders.ReplaceAddress(this.Response, address);
        return this.Partial(this.AnswerPartial, this);
    }

    /// <summary>
    /// Works out where the last row asks for the next rows as it comes into view: nowhere on a page that grows only
    /// when asked, when no organization follows the last row, or when the next ten would pass a hundred.
    /// </summary>
    /// <returns>The address, or <see langword="null"/>.</returns>
    private string? NextGrowth()
    {
        int next = this.Shown + Step;
        if (!this.GrowsOnScroll || !this.More || next > PageSize.Largest)
        {
            return null;
        }

        return this.Url.Page(this.PageName, new { shown = next, after = this.Last, scrolled = true });
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
