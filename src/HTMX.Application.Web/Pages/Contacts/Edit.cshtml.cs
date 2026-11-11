namespace HTMX.Application.Web.Pages.Contacts;

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// One contact's edit form, at /contacts/{id}/edit. A save that succeeds goes to the contact's page; a save that is
/// refused comes back with the form and the reason, as the organization's edit form does. The organization field, and
/// the handlers HTMX enhances it through, are <see cref="ContactFormModel{TInput}"/>'s.
/// </summary>
public sealed class EditModel : ContactFormModel<EditModel.InputModel>
{
    private const string EditPartial = "_ContactEdit";
    private const string DetailsPage = "./Details";

    // The form's name for the version it was filled from, as ModelState keys it.
    private const string VersionKey = nameof(Input) + "." + nameof(InputModel.Version);

    private readonly IContactService _contacts;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditModel"/> class.
    /// </summary>
    /// <param name="contacts">Reads the contact and saves the changes.</param>
    /// <param name="organizations">Reads the organizations the contact can belong to.</param>
    public EditModel(IContactService contacts, IOrganizationService organizations)
        : base(organizations)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        this._contacts = contacts;
    }

    /// <summary>
    /// Gets the contact as they are saved now, or <see langword="null"/> when the identifier names none.
    /// </summary>
    public ContactSingleResponse? Contact { get; private set; }

    /// <summary>
    /// Gets a value indicating whether another save overtook this one, so the form shows the saved values beside
    /// the ones being edited.
    /// </summary>
    public bool Overtaken { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the contact can be edited: they and their organization are active.
    /// </summary>
    public bool Changeable => this.Contact is ContactSingleResponse contact
        && contact.Archived is null
        && contact.Organization.ArchivedOn is null;

    /// <summary>
    /// Gets the form's fields, ready to draw, or <see langword="null"/> when the contact cannot be edited.
    /// </summary>
    public ContactFields? Fields { get; private set; }

    /// <inheritdoc/>
    protected override string PageName => "./Edit";

    /// <summary>
    /// Fills the form from the contact the route names.
    /// </summary>
    /// <param name="id">The contact's identifier, from the route.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The page, which says so when no contact has the identifier.</returns>
    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        ContactSingleResponse? contact = await this.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        if (contact is null)
        {
            this.Response.StatusCode = StatusCodes.Status404NotFound;
            return this.Page();
        }

        this.Input = CreateInput(contact);
        await this.PrepareFieldsAsync(enhanced: false, cancellationToken).ConfigureAwait(false);
        return this.Page();
    }

    /// <summary>
    /// Saves the form. A save that succeeds, or that changes nothing, goes to the contact's page, which also says so
    /// when the contact no longer exists. A refused save answers 422 when a value must change and 409 when the contact
    /// has moved on or can no longer be edited, with the form and the reason.
    /// </summary>
    /// <param name="id">The contact's identifier, from the route.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>Where to go next, or the form again.</returns>
    public async Task<IActionResult> OnPostAsync(long id, CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return await this.RefuseAsync(id, StatusCodes.Status422UnprocessableEntity, cancellationToken)
                .ConfigureAwait(false);
        }

        ContactUpdateRequest request = this.CreateRequest(id);
        UpdateOutcome outcome = await this._contacts.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
        return outcome switch
        {
            UpdateOutcome.OrganizationUnavailable =>
                await this.RefuseOrganizationAsync(id, cancellationToken).ConfigureAwait(false),
            UpdateOutcome.Overtaken =>
                await this.RefuseOvertakenAsync(id, cancellationToken).ConfigureAwait(false),
            UpdateOutcome.Archived =>
                await this.RefuseAsync(id, StatusCodes.Status409Conflict, cancellationToken).ConfigureAwait(false),
            _ =>
                this.ShowContact(id),
        };
    }

    /// <summary>
    /// Fills the form from the contact as they are saved.
    /// </summary>
    /// <param name="contact">The contact.</param>
    /// <returns>The form's values.</returns>
    private static InputModel CreateInput(ContactSingleResponse contact)
    {
        return new InputModel
        {
            Version = contact.Version.Value,
            Name = contact.Name.Value,
            JobTitle = contact.JobTitle?.Value,
            Email = contact.Email?.Value,
            Phone = contact.Phone?.Value,
            Notes = contact.Notes?.Value,
            OrganizationId = contact.Organization.Id.Value,
        };
    }

    /// <summary>
    /// Turns the form into a change, once it has passed validation: the values trimmed, an empty value as none, and
    /// the signed-in user as who made it.
    /// </summary>
    /// <param name="id">The contact's identifier.</param>
    /// <returns>The change.</returns>
    private ContactUpdateRequest CreateRequest(long id)
    {
        InputModel input = this.Input;
        long organization = input.OrganizationId
            ?? throw new InvalidOperationException("A valid form always has an organization.");
        string? jobTitle = Optional(input.JobTitle);
        string? email = Optional(input.Email);
        string? phone = Optional(input.Phone);
        string? notes = Optional(input.Notes);
        return new ContactUpdateRequest
        {
            Id = new ContactId(id),
            Version = new VersionNumber(input.Version),
            Name = new DisplayName(input.Name.Trim()),
            JobTitle = jobTitle is null ? null : new JobTitle(jobTitle),
            Email = email is null ? null : new EmailAddress(email),
            Phone = phone is null ? null : new PhoneNumber(phone),
            Notes = notes is null ? null : new Notes(notes),
            Organization = new OrganizationId(organization),
            ChangedBy = SignedInUser.Of(this.User),
        };
    }

    /// <summary>
    /// Reads the contact as they are saved now, for the page's header and, after a conflict, the saved values.
    /// </summary>
    /// <param name="id">The contact's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The contact, or <see langword="null"/> when there is none.</returns>
    private async Task<ContactSingleResponse?> ReadAsync(long id, CancellationToken cancellationToken)
    {
        this.Id = id;
        this.Contact = await this._contacts.FindAsync(new ContactId(id), cancellationToken).ConfigureAwait(false);
        return this.Contact;
    }

    /// <summary>
    /// Gets the form's fields ready to draw, unless the contact cannot be edited, when the page shows why instead.
    /// </summary>
    /// <param name="enhanced">Whether HTMX sent the request, so the relation field can be drawn at once.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>A task that completes when the fields are ready.</returns>
    private async Task PrepareFieldsAsync(bool enhanced, CancellationToken cancellationToken)
    {
        if (!this.Changeable)
        {
            return;
        }

        ContactSingleResponse? saved = this.Overtaken ? this.Contact : null;
        this.Fields = await this.CreateFieldsAsync(saved, enhanced, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Marks the organization as one that takes no one in, and answers with the form.
    /// </summary>
    /// <param name="id">The contact's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The form, with the status 422.</returns>
    private Task<IActionResult> RefuseOrganizationAsync(long id, CancellationToken cancellationToken)
    {
        this.MarkOrganizationUnavailable();
        return this.RefuseAsync(id, StatusCodes.Status422UnprocessableEntity, cancellationToken);
    }

    /// <summary>
    /// Says that another save reached the contact first, and answers with the form, which keeps the values being
    /// edited, shows the saved ones beside them, and now starts from the saved version, so that saving again replaces
    /// it on purpose.
    /// </summary>
    /// <param name="id">The contact's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The form, with the status 409.</returns>
    private async Task<IActionResult> RefuseOvertakenAsync(long id, CancellationToken cancellationToken)
    {
        ContactSingleResponse? saved = await this.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        if (saved is null)
        {
            return this.ShowContact(id);
        }

        // The save that overtook this one may be the same user's, from another tab, so the message names who made it.
        UserReference editor = saved.LastModified.By;
        string message = $"{editor.Name.Value} saved this contact while you were editing them. Your changes are "
            + "still here, and the saved values are shown under the fields that differ. Save again only if yours "
            + "should replace theirs.";
        this.ModelState.AddModelError(string.Empty, message);

        // A tag helper writes the value that was posted ahead of the model's, so the posted version is forgotten first.
        this.ModelState.Remove(VersionKey);
        this.Input.Version = saved.Version.Value;
        this.Overtaken = true;
        return await this.AnswerAsync(StatusCodes.Status409Conflict, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Answers a refused save with the form and its messages, after reading the contact as they are saved now.
    /// </summary>
    /// <param name="id">The contact's identifier.</param>
    /// <param name="statusCode">The status that says why the save was refused.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The form, or the contact's page when they no longer exist.</returns>
    private async Task<IActionResult> RefuseAsync(long id, int statusCode, CancellationToken cancellationToken)
    {
        ContactSingleResponse? saved = await this.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        if (saved is null)
        {
            return this.ShowContact(id);
        }

        return await this.AnswerAsync(statusCode, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Answers a refused save with the edit page's content when HTMX sent it, to swap into the page it already shows,
    /// and with the whole page otherwise. HTMX is then known to be there, so its answer draws the relation field at
    /// once.
    /// </summary>
    /// <param name="statusCode">The status that says why the save was refused.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The content or the page.</returns>
    private async Task<IActionResult> AnswerAsync(int statusCode, CancellationToken cancellationToken)
    {
        this.Response.StatusCode = statusCode;
        bool enhanced = HtmxHeaders.IsPartialRequest(this.Request);
        await this.PrepareFieldsAsync(enhanced, cancellationToken).ConfigureAwait(false);
        return HtmxAnswers.PartOrPage(this, EditPartial);
    }

    /// <summary>
    /// Goes to the contact's page: by a redirect without HTMX, and by asking HTMX to load it with HTMX.
    /// </summary>
    /// <param name="id">The contact's identifier.</param>
    /// <returns>The redirect, or 204 No Content with the request to load the page.</returns>
    private IActionResult ShowContact(long id)
    {
        return HtmxAnswers.NextPage(this, DetailsPage, new { id });
    }

    /// <summary>
    /// The form's values, as the browser posted them: a contact's values, and the version they were filled from.
    /// </summary>
    public sealed class InputModel : ContactInput
    {
        /// <summary>
        /// Gets or sets the version the form was filled from, which the form carries in a hidden field.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Version { get; set; }
    }
}
