namespace HTMX.Application.Web.Pages.Contacts;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// What every form for a contact's values shares: the form's values, and the organization field, which starts as a
/// select of every active organization and is enhanced by HTMX into the brand's relation field and its picker.
/// </summary>
/// <remarks>
/// The select works without HTMX. With HTMX it loads the relation field in its place, whose picker filters the
/// organizations as you type: two GET handlers here answer it, one with the field and the picker, one with the picker's
/// list alone. Razor Pages finds them on each form that inherits them.
/// </remarks>
/// <typeparam name="TInput">The form's values: a contact's values, and whatever else the form carries.</typeparam>
public abstract class ContactFormModel<TInput> : PageModel
    where TInput : ContactInput, new()
{
    private const string FieldAnswerPartial = "_OrganizationFieldAnswer";
    private const string ChoicesPartial = "_OrganizationChoices";
    private const string FieldHandler = "OrganizationField";
    private const string ChoicesHandler = "OrganizationChoices";

    // How many organizations the picker lists at once; a filter narrows them to the one wanted.
    private const int ChoicesShown = 10;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactFormModel{TInput}"/> class.
    /// </summary>
    /// <param name="organizations">Reads the organizations a contact can belong to.</param>
    protected ContactFormModel(IOrganizationService organizations)
    {
        ArgumentNullException.ThrowIfNull(organizations);
        this.Organizations = organizations;
    }

    /// <summary>
    /// Gets or sets the form's values, as the browser posted them. Model binding writes them on a post.
    /// </summary>
    [BindProperty]
    public TInput Input { get; set; } = new TInput();

    /// <summary>
    /// Gets or sets the identifier the route names.
    /// </summary>
    public long Id { get; protected set; }

    /// <summary>
    /// Gets the organization field the field handler answers with, or that an answer to HTMX draws at once.
    /// </summary>
    public OrganizationField? Relation { get; private set; }

    /// <summary>
    /// Gets the organization picker drawn beside the relation field, or <see langword="null"/> while the page holds the
    /// select instead.
    /// </summary>
    public OrganizationPicker? Picker { get; private set; }

    /// <summary>
    /// Gets the form's page, such as <c>./Edit</c>, which the field's handlers belong to.
    /// </summary>
    protected abstract string PageName { get; }

    /// <summary>
    /// Gets the service the form reads the organizations through.
    /// </summary>
    protected IOrganizationService Organizations { get; }

    /// <summary>
    /// Answers HTMX with the relation field for an organization, and the picker beside it: as the page loads, in place
    /// of the select, and after a choice in the picker, which the new picker closes by replacing it.
    /// </summary>
    /// <param name="id">The contact's identifier, from the route.</param>
    /// <param name="organization">The organization chosen, if any.</param>
    /// <param name="choose">Whether the picker's Choose sent it, so the field's button takes the focus back.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The field, and the picker for the layout to put in its place.</returns>
    public async Task<IActionResult> OnGetOrganizationFieldAsync(
        long id,
        long? organization,
        bool choose,
        CancellationToken cancellationToken)
    {
        this.Id = id;
        this.Input = new TInput { OrganizationId = organization };
        await this.EnhanceAsync(saved: null, choose, cancellationToken).ConfigureAwait(false);
        OrganizationFieldAnswer answer = new OrganizationFieldAnswer
        {
            Field = this.Relation ?? throw new InvalidOperationException("The field was just made."),
            Picker = this.Picker ?? throw new InvalidOperationException("The picker was just made."),
        };
        return this.Partial(FieldAnswerPartial, answer);
    }

    /// <summary>
    /// Answers HTMX with the picker's list for a filter, the chosen organization's radio checked when it is listed.
    /// </summary>
    /// <param name="id">The contact's identifier, from the route.</param>
    /// <param name="filter">The text the names must contain, as typed.</param>
    /// <param name="organization">The organization whose radio was checked, if any.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The list.</returns>
    public async Task<IActionResult> OnGetOrganizationChoicesAsync(
        long id,
        string? filter,
        long? organization,
        CancellationToken cancellationToken)
    {
        this.Id = id;
        this.Input = new TInput { OrganizationId = organization };
        OrganizationPicker picker = await this.CreatePickerAsync(filter, cancellationToken).ConfigureAwait(false);
        return this.Partial(ChoicesPartial, picker);
    }

    /// <summary>
    /// Makes the form's fields, with the organization field as the select HTMX enhances or, when HTMX sent the request,
    /// as the relation field and its picker straight away.
    /// </summary>
    /// <param name="saved">The contact as another save left them, or <see langword="null"/> when none overtook this
    /// one.</param>
    /// <param name="enhanced">Whether HTMX sent the request, so the relation field can be drawn at once.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The fields.</returns>
    protected async Task<ContactFields> CreateFieldsAsync(
        ContactSingleResponse? saved,
        bool enhanced,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SelectListItem> options = [];
        if (enhanced)
        {
            await this.EnhanceAsync(saved, focus: false, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            options = await this.CreateOptionsAsync(cancellationToken).ConfigureAwait(false);
        }

        string enhance = this.HandlerAddress(
            FieldHandler,
            new { id = this.Id, organization = this.Input.OrganizationId });
        return new ContactFields
        {
            Input = this.Input,
            Saved = saved,
            Relation = enhanced ? this.Relation : null,
            OrganizationOptions = options,
            EnhanceAction = enhance,
        };
    }

    /// <summary>
    /// Writes the address of one of the form page's handlers.
    /// </summary>
    /// <param name="handler">The handler, such as <c>OrganizationField</c>.</param>
    /// <param name="values">The route's and query string's values, the identifier among them.</param>
    /// <returns>The address.</returns>
    protected string HandlerAddress(string handler, object values)
    {
        return this.Url.Page(this.PageName, handler, values)
            ?? throw new InvalidOperationException($"The handler {handler} has no address.");
    }

    /// <summary>
    /// Makes the relation field for the organization the form names, and the picker beside it.
    /// </summary>
    /// <param name="saved">The contact as another save left them, or <see langword="null"/> when none overtook this
    /// one.</param>
    /// <param name="focus">Whether the field's button takes the focus.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>A task that completes when both are made.</returns>
    private async Task EnhanceAsync(ContactSingleResponse? saved, bool focus, CancellationToken cancellationToken)
    {
        OrganizationReference? chosen = await this.FindChosenAsync(cancellationToken).ConfigureAwait(false);
        OrganizationReference? savedOrganization = saved?.Organization;
        bool moved = savedOrganization is not null && savedOrganization.Id.Value != this.Input.OrganizationId;
        this.Relation = new OrganizationField
        {
            Input = this.Input,
            Chosen = chosen,
            Saved = moved ? savedOrganization?.Name.Value : null,
            Focus = focus,
        };
        this.Picker = await this.CreatePickerAsync(filter: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the organization the form names.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The organization, or <see langword="null"/> when the form names none, or one that does not
    /// exist.</returns>
    private async Task<OrganizationReference?> FindChosenAsync(CancellationToken cancellationToken)
    {
        if (this.Input.OrganizationId is not long chosen)
        {
            return null;
        }

        OrganizationSingleResponse? organization = await this.Organizations
            .FindAsync(new OrganizationId(chosen), cancellationToken)
            .ConfigureAwait(false);
        return organization is null
            ? null
            : new OrganizationReference
            {
                Id = organization.Id,
                Name = organization.Name,
                ArchivedOn = organization.Archived?.On,
            };
    }

    /// <summary>
    /// Reads the first active organizations, by name, whose names contain the filter, for the picker.
    /// </summary>
    /// <param name="filter">The text the names must contain, as typed, if any.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The picker.</returns>
    private async Task<OrganizationPicker> CreatePickerAsync(string? filter, CancellationToken cancellationToken)
    {
        OrganizationCollectionRequest request = new OrganizationCollectionRequest
        {
            Filter = string.IsNullOrWhiteSpace(filter) ? null : new FilterText(filter),
            Status = RecordStatus.Active,
            Sort = OrganizationSortColumn.Name,
            Direction = SortDirection.Ascending,
            PageSize = new PageSize(ChoicesShown),
        };
        OrganizationCollectionResponse choices = await this.Organizations
            .ListAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return new OrganizationPicker
        {
            Choices = choices,
            Chosen = this.Input.OrganizationId is long chosen ? new OrganizationId(chosen) : null,
            ChoicesAction = this.HandlerAddress(ChoicesHandler, new { id = this.Id }),
            FieldAction = this.HandlerAddress(FieldHandler, new { id = this.Id }),
        };
    }

    /// <summary>
    /// Reads every active organization as an option of the select.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The options, by name.</returns>
    private async Task<IReadOnlyList<SelectListItem>> CreateOptionsAsync(CancellationToken cancellationToken)
    {
        OrganizationReference[] organizations = await this.Organizations
            .ListChoicesAsync(cancellationToken)
            .ConfigureAwait(false);
        return
        [
            .. organizations.Select(static organization => new SelectListItem(
                organization.Name.Value,
                organization.Id.Value.ToString(CultureInfo.InvariantCulture))),
        ];
    }
}
