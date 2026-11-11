namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// One organization's edit form, at /organizations/{id}/edit. A save that succeeds goes to the organization's page; a
/// save that is refused comes back with the form and the reason. Without HTMX that is a redirect or the whole page;
/// with HTMX it is a request to load the organization's page, or only the edit page's content.
/// </summary>
public sealed class EditModel : PageModel
{
    private const string EditPartial = "_OrganizationEdit";
    private const string DetailsPage = "./Details";

    // The form's names for its fields, as ModelState keys them.
    private const string NameKey = nameof(Input) + "." + nameof(InputModel.Name);
    private const string VersionKey = nameof(Input) + "." + nameof(InputModel.Version);

    private readonly IOrganizationService _organizations;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditModel"/> class.
    /// </summary>
    /// <param name="organizations">Reads the organization and saves the changes.</param>
    public EditModel(IOrganizationService organizations)
    {
        ArgumentNullException.ThrowIfNull(organizations);
        this._organizations = organizations;
    }

    /// <summary>
    /// Gets or sets the form's values, as the browser posted them. Model binding writes them on a post; the GET handler
    /// fills them from the organization.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    /// <summary>
    /// Gets the identifier the route names.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// Gets the organization as it is saved now, or <see langword="null"/> when the identifier names none.
    /// </summary>
    public OrganizationSingleResponse? Organization { get; private set; }

    /// <summary>
    /// Gets a value indicating whether another save overtook this one, so the form shows the saved values beside
    /// the ones being edited.
    /// </summary>
    public bool Overtaken { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the organization was archived after the form was shown, so the alert that says
    /// it can't be edited takes the keyboard in place of the form.
    /// </summary>
    public bool ArchivedMeanwhile { get; private set; }

    /// <summary>
    /// Fills the form from the organization the route names.
    /// </summary>
    /// <param name="id">The organization's identifier, from the route.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The page, which says so when no organization has the identifier.</returns>
    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        OrganizationSingleResponse? organization = await this.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            this.Response.StatusCode = StatusCodes.Status404NotFound;
            return this.Page();
        }

        this.Input = CreateInput(organization);
        return this.Page();
    }

    /// <summary>
    /// Saves the form. A save that succeeds, or that changes nothing, goes to the organization's page, which also says
    /// so when the organization no longer exists. A refused save answers 422 when a value must change and 409 when
    /// the organization has moved on, with the form and the reason.
    /// </summary>
    /// <param name="id">The organization's identifier, from the route.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>Where to go next, or the form again.</returns>
    public async Task<IActionResult> OnPostAsync(long id, CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return await this.RefuseAsync(id, StatusCodes.Status422UnprocessableEntity, cancellationToken)
                .ConfigureAwait(false);
        }

        OrganizationUpdateRequest request = this.CreateRequest(id);
        UpdateOutcome outcome = await this._organizations.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
        return outcome switch
        {
            UpdateOutcome.NameTaken =>
                await this.RefuseNameAsync(id, cancellationToken).ConfigureAwait(false),
            UpdateOutcome.Overtaken =>
                await this.RefuseOvertakenAsync(id, cancellationToken).ConfigureAwait(false),
            UpdateOutcome.Archived =>
                await this.RefuseArchivedAsync(id, cancellationToken).ConfigureAwait(false),
            _ =>
                this.ShowOrganization(id),
        };
    }

    /// <summary>
    /// Fills the form from the organization as it is saved.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>The form's values.</returns>
    private static InputModel CreateInput(OrganizationSingleResponse organization)
    {
        return new InputModel
        {
            Version = organization.Version.Value,
            Name = organization.Name.Value,
            Industry = organization.Industry,
            Website = organization.Website?.Value,
        };
    }

    /// <summary>
    /// Turns the form into a change, once it has passed validation: the values trimmed, an empty website as none, and
    /// the signed-in user as who made it.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <returns>The change.</returns>
    private OrganizationUpdateRequest CreateRequest(long id)
    {
        InputModel input = this.Input;
        string? website = input.Website?.Trim();
        return new OrganizationUpdateRequest
        {
            Id = new OrganizationId(id),
            Version = new VersionNumber(input.Version),
            Name = new OrganizationName(input.Name.Trim()),
            Industry = input.Industry ?? throw new InvalidOperationException("A valid form always has an industry."),
            Website = string.IsNullOrEmpty(website) ? null : new WebAddress(website),
            ChangedBy = SignedInUser.Of(this.User),
        };
    }

    /// <summary>
    /// Says that the organization was archived after the form was shown, and answers with the alert that says it can't
    /// be edited, which takes the keyboard, since the form and its Save button are gone.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The edit page's content, with the status 409.</returns>
    private Task<IActionResult> RefuseArchivedAsync(long id, CancellationToken cancellationToken)
    {
        this.ArchivedMeanwhile = true;
        return this.RefuseAsync(id, StatusCodes.Status409Conflict, cancellationToken);
    }

    /// <summary>
    /// Reads the organization as it is saved now, for the page's header and, after a conflict, the saved values.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The organization, or <see langword="null"/> when there is none.</returns>
    private async Task<OrganizationSingleResponse?> ReadAsync(long id, CancellationToken cancellationToken)
    {
        this.Id = id;
        OrganizationId organizationId = new OrganizationId(id);
        this.Organization = await this._organizations
            .FindAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        return this.Organization;
    }

    /// <summary>
    /// Marks the name as taken by another organization, and answers with the form.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The form, with the status 422.</returns>
    private Task<IActionResult> RefuseNameAsync(long id, CancellationToken cancellationToken)
    {
        // Names match however they are capitalized, so the message does not repeat the one typed.
        this.ModelState.AddModelError(NameKey, "Another organization already has this name.");
        return this.RefuseAsync(id, StatusCodes.Status422UnprocessableEntity, cancellationToken);
    }

    /// <summary>
    /// Says that another save reached the organization first, and answers with the form, which keeps the values being
    /// edited, shows the saved ones beside them, and now starts from the saved version, so that saving again replaces
    /// it on purpose.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The form, with the status 409.</returns>
    private async Task<IActionResult> RefuseOvertakenAsync(long id, CancellationToken cancellationToken)
    {
        OrganizationSingleResponse? saved = await this.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        if (saved is null)
        {
            return this.ShowOrganization(id);
        }

        // The save that overtook this one may be the same user's, from another tab, so the message names who made it.
        UserReference editor = saved.LastModified.By;
        string message = $"{editor.Name.Value} saved this organization while you were editing it. Your changes are "
            + "still here, and the saved values are shown under the fields that differ. Save again only if yours "
            + "should replace theirs.";
        this.ModelState.AddModelError(string.Empty, message);

        // A tag helper writes the value that was posted ahead of the model's, so the posted version is forgotten first.
        this.ModelState.Remove(VersionKey);
        this.Input.Version = saved.Version.Value;
        this.Overtaken = true;
        return this.Answer(StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// Answers a refused save with the form and its messages, after reading the organization as it is saved now.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="statusCode">The status that says why the save was refused.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The form, or the organization's page when it no longer exists.</returns>
    private async Task<IActionResult> RefuseAsync(long id, int statusCode, CancellationToken cancellationToken)
    {
        OrganizationSingleResponse? saved = await this.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        return saved is null ? this.ShowOrganization(id) : this.Answer(statusCode);
    }

    /// <summary>
    /// Answers a refused save with the edit page's content when HTMX sent it, to swap into the page it already shows,
    /// and with the whole page otherwise.
    /// </summary>
    /// <param name="statusCode">The status that says why the save was refused.</param>
    /// <returns>The content or the page.</returns>
    private IActionResult Answer(int statusCode)
    {
        this.Response.StatusCode = statusCode;
        return HtmxAnswers.PartOrPage(this, EditPartial);
    }

    /// <summary>
    /// Goes to the organization's page: by a redirect without HTMX, and by asking HTMX to load it with HTMX.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <returns>The redirect, or 204 No Content with the request to load the page.</returns>
    private IActionResult ShowOrganization(long id)
    {
        return HtmxAnswers.NextPage(this, DetailsPage, new { id });
    }

    /// <summary>
    /// The form's values, as the browser posted them: an organization's values, and the version they were filled from.
    /// </summary>
    public sealed class InputModel : OrganizationInput
    {
        /// <summary>
        /// Gets or sets the version the form was filled from, which the form carries in a hidden field.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Version { get; set; }
    }
}
