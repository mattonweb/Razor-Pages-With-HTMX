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

    /// <inheritdoc/>
    protected override string PageName => "./Create";

    /// <summary>
    /// Starts an empty form. When it was opened from an organization that takes people in, that organization is
    /// chosen; an archived one is not, since saving would refuse it.
    /// </summary>
    /// <param name="organization">The organization the form was opened from, from the query string, if any.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The page.</returns>
    public async Task<IActionResult> OnGetAsync(long? organization, CancellationToken cancellationToken)
    {
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
        string? phone = Optional(input.Phone);
        string? notes = Optional(input.Notes);
        return new ContactCreateRequest
        {
            Name = new DisplayName(input.Name.Trim()),
            JobTitle = jobTitle is null ? null : new JobTitle(jobTitle),
            Email = email is null ? null : new EmailAddress(email),
            Phone = phone is null ? null : new PhoneNumber(phone),
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
}
