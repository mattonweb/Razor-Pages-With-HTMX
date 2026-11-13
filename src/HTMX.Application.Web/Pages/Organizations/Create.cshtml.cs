namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using HTMX.Application.Web.TagHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The form that creates an organization, at /organizations/create. A save that succeeds goes to the new
/// organization's page; a save that is refused comes back with the form and the reason. Without HTMX that is a
/// redirect or the whole page; with HTMX it is a request to load the new organization's page, or only the form.
/// With HTMX the name is also checked as it is typed.
/// </summary>
public sealed class CreateModel : PageModel
{
    private const string CreatePage = "./Create";
    private const string FormPartial = "_OrganizationCreateForm";
    private const string NameFieldPartial = "_OrganizationNameField";
    private const string CheckNameHandler = "CheckName";
    private const string DetailsPage = "./Details";

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
    /// Gets the address the form's name is checked at as it is typed: this page's own, with the CheckName handler.
    /// </summary>
    public string? CheckNameAddress => this.Url.Page(CreatePage, CheckNameHandler);

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the page, as by a link,
    /// since HTMX moves the keyboard nowhere itself, and not when the browser loaded it on its own or a refused save
    /// drew the form again.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Starts an empty form.
    /// </summary>
    public void OnGet()
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
    }

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
            OrganizationNameCheck.MarkTaken(this.ModelState);
            return this.Refuse();
        }

        OrganizationId created = response.Id
            ?? throw new InvalidOperationException("A created organization always has an identifier.");
        return HtmxAnswers.NextPage(this, DetailsPage, new { id = created.Value });
    }

    /// <summary>
    /// Checks the name as it is typed, and answers with the Name field alone, which HTMX morphs into the form. It saves
    /// nothing, so it answers 200 whatever it finds, and it moves no focus. Only HTMX sends it; without HTMX the save
    /// checks the name.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The Name field, with its message.</returns>
    public async Task<IActionResult> OnPostCheckNameAsync(CancellationToken cancellationToken)
    {
        await OrganizationNameCheck
            .CheckAsync(this._organizations, this.Input, except: null, this.ModelState, cancellationToken)
            .ConfigureAwait(false);
        InvalidControlTagHelper.LeaveFocusAlone(this.HttpContext);
        OrganizationFields field = new OrganizationFields
        {
            Input = this.Input,
            CheckNameAddress = this.CheckNameAddress,
        };
        return this.Partial(NameFieldPartial, field);
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
