namespace HTMX.Application.Web.Pages.Contacts;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// What every form for a contact's values shares: the form's values, and the organization field, which starts as a
/// select of every active organization and is enhanced by HTMX into the brand's relation field and its picker.
/// </summary>
/// <remarks>
/// The select works without HTMX. With HTMX it loads the relation field in its place, whose picker filters the
/// organizations as you type: two GET handlers here answer it, one with the field and the picker, one with the picker's
/// list alone. Razor Pages finds them on each form that inherits them: the form that edits a contact, whose addresses
/// carry the contact's identifier, and the form that creates one, whose addresses carry none.
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

    // The form's name for the organization field, as ModelState keys it.
    private const string OrganizationKey = nameof(Input) + "." + nameof(ContactInput.OrganizationId);

    // The telephone number field's partial, and the handlers its Add and Remove post the form to.
    private const string PhoneNumbersPartial = "_PhoneNumberField";
    private const string AddPhoneNumberHandler = "AddPhoneNumber";
    private const string RemovePhoneNumberHandler = "RemovePhoneNumber";

    // The form's name for the telephone numbers, as ModelState keys it.
    private const string PhoneNumbersKey = nameof(Input) + "." + nameof(ContactInput.PhoneNumbers);

    // The row that takes the keyboard when the field is drawn: the row Add just made, or the row in the place of the
    // one Remove just took away. None when the field is drawn for any other reason.
    private int? _focusedPhoneNumber;

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
    /// Gets or sets the identifier of the contact the form saves, which the route names, or <see langword="null"/> on a
    /// form that creates one. The field's addresses carry it, so that they answer to the page that drew the field.
    /// </summary>
    public long? Id { get; protected set; }

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
    /// Gets the route's and query string's values the form posts with, such as the contact's identifier, which the
    /// telephone number field's Add and Remove post with too.
    /// </summary>
    protected abstract object FormValues { get; }

    /// <summary>
    /// Gets the service the form reads the organizations through.
    /// </summary>
    protected IOrganizationService Organizations { get; }

    /// <summary>
    /// Answers HTMX with the relation field for an organization, and the picker beside it: as the page loads, in place
    /// of the select, and after a choice in the picker, which the new picker closes by replacing it.
    /// </summary>
    /// <param name="id">The contact's identifier, from the route, or <see langword="null"/> on a form that creates
    /// one.</param>
    /// <param name="organization">The organization chosen, if any.</param>
    /// <param name="choose">Whether the picker's Choose sent it, so the field's button takes the focus back.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The field, and the picker for the layout to put in its place.</returns>
    public async Task<IActionResult> OnGetOrganizationFieldAsync(
        long? id,
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
    /// <param name="id">The contact's identifier, from the route, or <see langword="null"/> on a form that creates
    /// one.</param>
    /// <param name="filter">The text the names must contain, as typed.</param>
    /// <param name="organization">The organization whose radio was checked, if any.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The list.</returns>
    public async Task<IActionResult> OnGetOrganizationChoicesAsync(
        long? id,
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
    /// Reads a value the form may leave empty: trimmed, and an empty value as none.
    /// </summary>
    /// <param name="text">The value as posted.</param>
    /// <returns>The value, or <see langword="null"/> when it is empty.</returns>
    protected static string? Optional(string? text)
    {
        string? trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
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
            PhoneNumbers = this.CreatePhoneNumberField(saved),
        };
    }

    /// <summary>
    /// Gives the form one more empty row for a telephone number, which takes the keyboard, unless the form already
    /// holds the most a contact can have. Nothing is saved, so nothing is checked: the messages from model binding go.
    /// </summary>
    protected void AddPhoneNumberRow()
    {
        this.ModelState.Clear();
        string?[] numbers = this.Input.PhoneNumbers;
        if (numbers.Length < TelephoneNumbersAttribute.Most)
        {
            this.Input.PhoneNumbers = [.. numbers, null];
        }

        this._focusedPhoneNumber = this.Input.PhoneNumbers.Length - 1;
    }

    /// <summary>
    /// Takes a row away from the form and gives the keyboard to the row now in its place, or to the new last row when
    /// it was last. As with Add, nothing is saved or checked.
    /// </summary>
    /// <param name="position">The row's position, from 0, which its Remove button posted.</param>
    protected void RemovePhoneNumberRow(int position)
    {
        this.ModelState.Clear();
        List<string?> numbers = [.. this.Input.PhoneNumbers];
        if (position >= 0 && position < numbers.Count)
        {
            numbers.RemoveAt(position);
        }

        this.Input.PhoneNumbers = [.. numbers];
        this._focusedPhoneNumber = Math.Clamp(position, 0, Math.Max(numbers.Count - 1, 0));
    }

    /// <summary>
    /// Answers HTMX's Add or Remove with the telephone number field alone, to swap in place of the field the page
    /// shows.
    /// </summary>
    /// <returns>The field.</returns>
    protected IActionResult PhoneNumberFieldAnswer()
    {
        return this.Partial(PhoneNumbersPartial, this.CreatePhoneNumberField(saved: null));
    }

    /// <summary>
    /// Reads the telephone numbers a save sends: each row's number trimmed, in order, with rows left empty left out.
    /// </summary>
    /// <returns>The numbers.</returns>
    protected PhoneNumbers WrittenPhoneNumbers()
    {
        string[] written = [.. this.Input.PhoneNumbers.Select(Optional).OfType<string>()];
        return new PhoneNumbers(Array.ConvertAll(written, static number => new PhoneNumber(number)));
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
    /// Marks the organization field with why a save was refused: the organization chosen is archived, and so takes no
    /// one in, or no longer exists.
    /// </summary>
    protected void MarkOrganizationUnavailable()
    {
        this.ModelState.AddModelError(
            OrganizationKey,
            "That organization is archived or no longer exists. Choose an active one.");
    }

    /// <summary>
    /// Joins numbers as the form shows them under the field after another save: trimmed, rows left empty left out.
    /// </summary>
    /// <param name="numbers">The numbers, as written or as saved.</param>
    /// <returns>The numbers, separated by commas, or <see langword="null"/> for none.</returns>
    private static string? Joined(IEnumerable<string?> numbers)
    {
        string joined = string.Join(", ", numbers.Select(Optional).OfType<string>());
        return joined.Length == 0 ? null : joined;
    }

    /// <summary>
    /// Gives the saved numbers to show under the field after another save, or nothing when they are the ones being
    /// edited.
    /// </summary>
    /// <param name="saved">The numbers the other save left.</param>
    /// <param name="edited">The numbers being edited, as the rows hold them.</param>
    /// <returns>The numbers to show, or <see langword="null"/> when there is nothing to compare.</returns>
    private static string? SavedHint(PhoneNumbers saved, string?[] edited)
    {
        string?[] savedValues = Array.ConvertAll(saved.Values, static number => (string?)number.Value);
        return SavedValue.Hint(Joined(savedValues), Joined(edited), "no telephone numbers");
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

    /// <summary>
    /// Makes the telephone number field from the form's values: a row for each, or one empty row when there are none,
    /// with the row that takes the keyboard marked and, after a save refused for its numbers, the rows that are wrong.
    /// </summary>
    /// <param name="saved">The contact as another save left them, or <see langword="null"/> when none overtook this
    /// one.</param>
    /// <returns>The field.</returns>
    private PhoneNumberField CreatePhoneNumberField(ContactSingleResponse? saved)
    {
        string?[] numbers = this.Input.PhoneNumbers.Length == 0 ? [null] : this.Input.PhoneNumbers;
        string? error = this.ModelState.TryGetValue(PhoneNumbersKey, out ModelStateEntry? entry)
            && entry.Errors.Count > 0
                ? entry.Errors[0].ErrorMessage
                : null;
        bool refused = error is not null;
        PhoneNumberRow[] rows = new PhoneNumberRow[numbers.Length];
        for (int position = 0; position < numbers.Length; position++)
        {
            rows[position] = new PhoneNumberRow
            {
                Value = numbers[position],
                Position = position,
                Focused = position == this._focusedPhoneNumber,
                Invalid = refused && !TelephoneNumbersAttribute.IsWellFormed(numbers[position]),
            };
        }

        string? savedNumbers = saved is null ? null : SavedHint(saved.PhoneNumbers, numbers);
        return new PhoneNumberField
        {
            Rows = rows,
            AddAction = this.HandlerAddress(AddPhoneNumberHandler, this.FormValues),
            RemoveAction = this.HandlerAddress(RemovePhoneNumberHandler, this.FormValues),
            CanAdd = numbers.Length < TelephoneNumbersAttribute.Most,
            Saved = savedNumbers,
            Error = error,
        };
    }
}
