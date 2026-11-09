namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The form that creates an organization, at /organizations/create. A save that succeeds goes to the new
/// organization's page; a save that is refused comes back with the form and the reason. Without HTMX that is a
/// redirect or the whole page; with HTMX it is a request to load the new organization's page, or only the form.
/// </summary>
public sealed class CreateModel : PageModel
{
    private const string FormPartial = "_OrganizationCreateForm";
    private const string DetailsPage = "./Details";

    // The form's name for the Name field, as ModelState keys it.
    private const string NameKey = nameof(Input) + "." + nameof(OrganizationInput.Name);

    private readonly IOrganizationService _organizations;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="organizations">Creates the organization.</param>
    public CreateModel(IOrganizationService organizations)
    {
        ArgumentNullException.ThrowIfNull(organizations);
        this._organizations = organizations;
    }

    /// <summary>
    /// Gets or sets the form's values, as the browser posted them. Model binding writes them on a post; the form
    /// starts empty.
    /// </summary>
    [BindProperty]
    public OrganizationInput Input { get; set; } = new OrganizationInput();

    /// <summary>
    /// Creates the organization. A form that fails validation, or names an organization that already exists, comes
    /// back with 422 and the messages.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The new organization's page, or the form again.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return this.Refuse();
        }

        OrganizationCreateRequest request = this.CreateRequest();
        OrganizationCreateResponse response = await this._organizations
            .CreateAsync(request, cancellationToken)
            .ConfigureAwait(false);
        if (response.Outcome == CreateOutcome.NameTaken)
        {
            // Names match however they are capitalized, so the message does not repeat the one typed.
            this.ModelState.AddModelError(NameKey, "Another organization already has this name.");
            return this.Refuse();
        }

        OrganizationId created = response.Id
            ?? throw new InvalidOperationException("A created organization always has an identifier.");
        return HtmxAnswers.NextPage(this, DetailsPage, new { id = created.Value });
    }

    /// <summary>
    /// Turns the form into a new organization, once it has passed validation: the values trimmed, an empty website as
    /// none, and the signed-in user as who created it.
    /// </summary>
    /// <returns>The new organization.</returns>
    private OrganizationCreateRequest CreateRequest()
    {
        OrganizationInput input = this.Input;
        string? website = input.Website?.Trim();
        return new OrganizationCreateRequest
        {
            Name = new OrganizationName(input.Name.Trim()),
            Industry = input.Industry ?? throw new InvalidOperationException("A valid form always has an industry."),
            Website = string.IsNullOrEmpty(website) ? null : new WebAddress(website),
            CreatedBy = SignedInUser.Of(this.User),
        };
    }

    /// <summary>
    /// Answers a refused save with 422 and the form: only the form when HTMX sent it, the whole page otherwise.
    /// </summary>
    /// <returns>The form or the page.</returns>
    private IActionResult Refuse()
    {
        this.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return HtmxAnswers.PartOrPage(this, FormPartial);
    }
}
