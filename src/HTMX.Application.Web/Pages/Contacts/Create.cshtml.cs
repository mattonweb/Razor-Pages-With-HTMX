namespace HTMX.Application.Web.Pages.Contacts;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// The form that creates a contact, at /contacts/create. Opened from an organization's Contacts tab, as
/// /contacts/create?organization=10003, it starts with that organization chosen and leads back to that tab. A save
/// that succeeds goes to the new contact's page; a save that is refused comes back with the form and the reason, as
/// the organization's create form does. The organization field, and the handlers HTMX enhances it through, are
/// <see cref="ContactFormModel{TInput}"/>'s.
/// </summary>
public sealed class CreateModel : ContactFormModel<ContactInput>
{
    private const string FormPartial = "_ContactCreateForm";
    private const string DetailsPage = "./Details";

    private readonly IContactService _contacts;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="contacts">Creates the contact.</param>
    /// <param name="organizations">Reads the organizations the contact can belong to.</param>
    public CreateModel(IContactService contacts, IOrganizationService organizations)
        : base(organizations)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        this._contacts = contacts;
    }

    /// <summary>
    /// Gets the organization the form was opened from, whose Contacts tab the page leads back to, or
    /// <see langword="null"/> when it was opened from none.
    /// </summary>
    public OrganizationSingleResponse? OpenedFrom { get; private set; }

    /// <summary>
    /// Gets the form's fields, ready to draw.
    /// </summary>
    public ContactFields? Fields { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the page, as by a link,
    /// since HTMX moves the keyboard nowhere itself, and not when the browser loaded it on its own or a refused save
    /// drew the form again.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <inheritdoc/>
    protected override string PageName => "./Create";

    /// <inheritdoc/>
    protected override object FormValues => new { organization = this.OpenedFrom?.Id.Value };

    /// <summary>
    /// Starts an empty form. When it was opened from an organization that takes people in, that organization is
    /// chosen; an archived one is not, since saving would refuse it.
    /// </summary>
    /// <param name="organization">The organization the form was opened from, from the query string, if any.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The page.</returns>
    public async Task<IActionResult> OnGetAsync(long? organization, CancellationToken cancellationToken)
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        await this.ReadOpenedFromAsync(organization, cancellationToken).ConfigureAwait(false);
        bool welcome = this.OpenedFrom is OrganizationSingleResponse from && from.Archived is null;
        this.Input = new ContactInput { OrganizationId = welcome ? organization : null };
        this.Fields = await this.CreateFieldsAsync(saved: null, enhanced: false, cancellationToken)
            .ConfigureAwait(false);
        return this.Page();
    }

    /// <summary>
    /// Creates the contact. A form that fails validation, or names an organization that takes no one in, comes back
    /// with 422 and the messages.
    /// </summary>
    /// <param name="organization">The organization the form was opened from, which its address keeps, if any.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The new contact's page, or the form again.</returns>
    public async Task<IActionResult> OnPostAsync(long? organization, CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return await this.RefuseAsync(organization, cancellationToken).ConfigureAwait(false);
        }

        ContactCreateRequest request = this.CreateRequest();
        ContactCreateResponse response = await this._contacts
            .CreateAsync(request, cancellationToken)
            .ConfigureAwait(false);
        if (response.Outcome == CreateOutcome.OrganizationUnavailable)
        {
            this.MarkOrganizationUnavailable();
            return await this.RefuseAsync(organization, cancellationToken).ConfigureAwait(false);
        }

        ContactId created = response.Id
            ?? throw new InvalidOperationException("A created contact always has an identifier.");
        return HtmxAnswers.NextPage(this, DetailsPage, new { id = created.Value });
    }

    /// <summary>
    /// Adds a row for a telephone number, ready to type in, and saves nothing. HTMX gets the telephone number field
    /// alone; without HTMX the form posts here and gets the whole page back, as it was posted, with the new row.
    /// </summary>
    /// <param name="organization">The organization the form was opened from, which its address keeps, if any.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The field or the page.</returns>
    public async Task<IActionResult> OnPostAddPhoneNumberAsync(long? organization, CancellationToken cancellationToken)
    {
        this.AddPhoneNumberRow();
        return await this.RedrawAsync(organization, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Takes a telephone number's row away, and saves nothing; the number goes when the form is saved. HTMX gets the
    /// telephone number field alone; without HTMX the form posts here and gets the whole page back.
    /// </summary>
    /// <param name="organization">The organization the form was opened from, which its address keeps, if any.</param>
    /// <param name="removePhoneNumber">The row's position, from 0, which its Remove button posts.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The field or the page.</returns>
    public async Task<IActionResult> OnPostRemovePhoneNumberAsync(
        long? organization,
        int removePhoneNumber,
        CancellationToken cancellationToken)
    {
        this.RemovePhoneNumberRow(removePhoneNumber);
        return await this.RedrawAsync(organization, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Turns the form into a new contact, once it has passed validation: the values trimmed, an empty value as none,
    /// and the signed-in user as who created them.
    /// </summary>
    /// <returns>The new contact.</returns>
    private ContactCreateRequest CreateRequest()
    {
        ContactInput input = this.Input;
        long organization = input.OrganizationId
            ?? throw new InvalidOperationException("A valid form always has an organization.");
        string? jobTitle = Optional(input.JobTitle);
        string? email = Optional(input.Email);
        string? notes = Optional(input.Notes);
        return new ContactCreateRequest
        {
            Name = new DisplayName(input.Name.Trim()),
            JobTitle = jobTitle is null ? null : new JobTitle(jobTitle),
            Email = email is null ? null : new EmailAddress(email),
            PhoneNumbers = this.WrittenPhoneNumbers(),
            Notes = notes is null ? null : new Notes(notes),
            Organization = new OrganizationId(organization),
            CreatedBy = SignedInUser.Of(this.User),
        };
    }

    /// <summary>
    /// Reads the organization the form was opened from, for the way back to its Contacts tab.
    /// </summary>
    /// <param name="organization">The organization's identifier, if the form was opened from one.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>A task that completes when it is read; it leaves none when the identifier names no
    /// organization.</returns>
    private async Task ReadOpenedFromAsync(long? organization, CancellationToken cancellationToken)
    {
        if (organization is not long id)
        {
            return;
        }

        this.OpenedFrom = await this.Organizations
            .FindAsync(new OrganizationId(id), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Answers a refused save with 422 and the form: only the form when HTMX sent it, with the relation field drawn at
    /// once since HTMX is known to be there, and the whole page otherwise.
    /// </summary>
    /// <param name="organization">The organization the form was opened from, if any.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The form or the page.</returns>
    private async Task<IActionResult> RefuseAsync(long? organization, CancellationToken cancellationToken)
    {
        this.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        await this.ReadOpenedFromAsync(organization, cancellationToken).ConfigureAwait(false);
        bool enhanced = HtmxHeaders.IsPartialRequest(this.Request);
        this.Fields = await this.CreateFieldsAsync(saved: null, enhanced, cancellationToken).ConfigureAwait(false);
        return HtmxAnswers.PartOrPage(this, FormPartial);
    }

    /// <summary>
    /// Draws the form again after Add or Remove, as it was posted: the telephone number field alone when HTMX sent the
    /// request, and the whole page otherwise, with the organization as the select, since HTMX is not there.
    /// </summary>
    /// <param name="organization">The organization the form was opened from, if any.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The field or the page.</returns>
    private async Task<IActionResult> RedrawAsync(long? organization, CancellationToken cancellationToken)
    {
        await this.ReadOpenedFromAsync(organization, cancellationToken).ConfigureAwait(false);
        if (HtmxHeaders.IsPartialRequest(this.Request))
        {
            return this.PhoneNumberFieldAnswer();
        }

        this.Fields = await this.CreateFieldsAsync(saved: null, enhanced: false, cancellationToken)
            .ConfigureAwait(false);
        return this.Page();
    }
}
