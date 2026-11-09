namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// One organization's own page, at /organizations/{id}: its name, status and version, who changed it last, and every
/// value it holds. An identifier that names no organization gets the page saying so, with the status 404 Not Found.
/// </summary>
public sealed class DetailsModel : PageModel
{
    private readonly IOrganizationService _organizations;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="organizations">Reads the organization.</param>
    public DetailsModel(IOrganizationService organizations)
    {
        ArgumentNullException.ThrowIfNull(organizations);
        this._organizations = organizations;
    }

    /// <summary>
    /// Gets or sets the route's values, as the browser sent them. Model binding writes them; the page only reads them.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public InputModel Input { get; set; } = new InputModel();

    /// <summary>
    /// Gets the organization the page shows, or <see langword="null"/> when the identifier names none.
    /// </summary>
    public OrganizationSingleResponse? Organization { get; private set; }

    /// <summary>
    /// Reads the organization the route names.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The page, which says so when no organization has the identifier.</returns>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        OrganizationId id = new OrganizationId(this.Input.Id);
        this.Organization = await this._organizations.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (this.Organization is null)
        {
            // The page still renders, inside the application's frame, and the status tells the browser too.
            this.Response.StatusCode = StatusCodes.Status404NotFound;
        }

        return this.Page();
    }

    /// <summary>
    /// The route's values, as the browser sent them.
    /// </summary>
    public sealed class InputModel
    {
        /// <summary>
        /// Gets or sets the identifier of the organization to show.
        /// </summary>
        public long Id { get; set; }
    }
}
