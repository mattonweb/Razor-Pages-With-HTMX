namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// The Contacts tab of an organization's page, at /organizations/{id}/contacts: the people who belong to it, in name
/// order, archived ones among the rest, ten to a page. Paging is key-based, as for the organizations list, and kept in
/// the query string. Each row's menu archives or restores its contact at once, says so in a toast whose Undo does the
/// opposite, and draws the list again as it was.
/// </summary>
/// <remarks>
/// Previous and Next change the URL, and the header's Archive and Restore post to the URL's values too, so with HTMX
/// they answer with the tab's whole content, header and all. An action on a row posts to the list's own address with
/// the handler and the contact added, so the POST binds where the list is just as the GET does, and reads the same
/// page again. It leaves the URL as it is, so with HTMX it answers with the list alone, and the rest of the tab stays;
/// without HTMX, the redirect goes back to the same page of the list.
/// </remarks>
public sealed class ContactsModel : OrganizationPageModel
{
    /// <summary>
    /// The query string name of the contact the page starts after: where Next goes.
    /// </summary>
    public const string AfterKey = "after";

    /// <summary>
    /// The query string name of the contact the page ends before: where Previous goes.
    /// </summary>
    public const string BeforeKey = "before";

    private const string ArchiveContactHandler = "ArchiveContact";
    private const string RestoreContactHandler = "RestoreContact";
    private const string ListChangedPartial = "_ContactListChanged";

    // What an action on a row, and its Undo, replaces: the list's card.
    private const string ListTarget = "#contact-list";

    // The query string name of the contact a row's action is for; the handlers' parameter has the same name.
    private const string ContactKey = "contact";

    private const int ContactsPerPage = 10;

    private readonly IContactService _contacts;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactsModel"/> class.
    /// </summary>
    /// <param name="organizations">Reads, archives and restores the organization.</param>
    /// <param name="contacts">Reads, archives and restores its contacts.</param>
    public ContactsModel(IOrganizationService organizations, IContactService contacts)
        : base(organizations)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        this._contacts = contacts;
    }

    /// <inheritdoc/>
    public override string PageName => "./Contacts";

    /// <inheritdoc/>
    public override string ContentPartial => "_OrganizationContacts";

    /// <summary>
    /// Gets or sets where the list is, from the query string, as the browser sent it. Model binding writes it, for a
    /// row's action as for the GET; the page only reads it.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public PagingInputModel Paging { get; set; } = new PagingInputModel();

    /// <summary>
    /// Gets the page of contacts to show, with the request it answers, or <see langword="null"/> until it is read.
    /// </summary>
    public ContactCollectionResponse? Contacts { get; private set; }

    /// <summary>
    /// Gets the contact a row's action was just for, whose menu button takes the focus when the list is drawn again,
    /// or <see langword="null"/> when no action has run.
    /// </summary>
    public ContactId? Focused { get; private set; }

    /// <summary>
    /// Archives one of the organization's contacts, and says so in a toast whose Undo restores them.
    /// </summary>
    /// <param name="contact">The contact's identifier, from the row's address.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The list and the toast for HTMX; a redirect back to the same page of the list otherwise; 400 for an
    /// identifier that cannot be one.</returns>
    public Task<IActionResult> OnPostArchiveContactAsync(long contact, CancellationToken cancellationToken)
    {
        return this.ChangeContactAsync(contact, ChangeKind.Archived, cancellationToken);
    }

    /// <summary>
    /// Restores one of the organization's archived contacts, and says so in a toast whose Undo archives them again.
    /// </summary>
    /// <param name="contact">The contact's identifier, from the row's address.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The list and the toast for HTMX; a redirect back to the same page of the list otherwise; 400 for an
    /// identifier that cannot be one.</returns>
    public Task<IActionResult> OnPostRestoreContactAsync(long contact, CancellationToken cancellationToken)
    {
        return this.ChangeContactAsync(contact, ChangeKind.Restored, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>Where the list is comes from the page the service answered with, which is the first page when the
    /// cursor's contact has gone.</remarks>
    public override Dictionary<string, string> TabValues()
    {
        return this.PageValues(this.Contacts?.Request.Cursor);
    }

    /// <summary>
    /// Builds the route values for a link to one page of the list, for the pager's Previous and Next.
    /// </summary>
    /// <param name="cursor">Where the page lies, or <see langword="null"/> for the first page.</param>
    /// <returns>The organization's identifier and, for any page but the first, the cursor.</returns>
    public Dictionary<string, string> PageValues(ContactCursor? cursor)
    {
        Dictionary<string, string> values = base.TabValues();
        if (cursor is ContactCursor named)
        {
            string key = named.Position == CursorPosition.After ? AfterKey : BeforeKey;
            values[key] = named.Id.Value.ToString(CultureInfo.InvariantCulture);
        }

        return values;
    }

    /// <summary>
    /// Writes the address a row's menu posts to: Archive for an active contact, Restore for an archived one, with
    /// where the list is.
    /// </summary>
    /// <param name="contact">The row's contact.</param>
    /// <returns>The address.</returns>
    public string RowAction(ContactSummary contact)
    {
        ArgumentNullException.ThrowIfNull(contact);
        string handler = contact.ArchivedOn is null ? ArchiveContactHandler : RestoreContactHandler;
        return this.ContactAddress(handler, contact.Id);
    }

    /// <inheritdoc/>
    protected override async Task ReadMoreAsync(OrganizationId id, CancellationToken cancellationToken)
    {
        ContactCollectionRequest request = new ContactCollectionRequest
        {
            Organization = id,
            Cursor = CreateCursor(this.Paging),
            PageSize = new PageSize(ContactsPerPage),
        };
        this.Contacts = await this._contacts.ListAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads where the page lies from the query string: after one contact, before one, or, with neither, at the start.
    /// An identifier that names none of the organization's contacts is the service's to deal with; it shows the first
    /// page.
    /// </summary>
    /// <param name="input">The query string, as bound.</param>
    /// <returns>The cursor, or <see langword="null"/> for the first page.</returns>
    private static ContactCursor? CreateCursor(PagingInputModel input)
    {
        if (input.After is long after)
        {
            return new ContactCursor(new ContactId(after), CursorPosition.After);
        }

        if (input.Before is long before)
        {
            return new ContactCursor(new ContactId(before), CursorPosition.Before);
        }

        return null;
    }

    /// <summary>
    /// Writes the address of one of the row handlers for a contact, with where the list is.
    /// </summary>
    /// <param name="handler">The handler, such as <c>ArchiveContact</c>.</param>
    /// <param name="contact">The contact.</param>
    /// <returns>The address.</returns>
    private string ContactAddress(string handler, ContactId contact)
    {
        Dictionary<string, string> values = this.TabValues();
        values[ContactKey] = contact.Value.ToString(CultureInfo.InvariantCulture);
        return this.HandlerAddress(handler, values);
    }

    /// <summary>
    /// Archives or restores a contact, reads the same page of the list again, and answers with it and a toast that
    /// says what happened. When the organization no longer exists, its page says so.
    /// </summary>
    /// <param name="contact">The contact's identifier, as sent.</param>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The list and the toast for HTMX; a redirect back to the same page of the list otherwise.</returns>
    private async Task<IActionResult> ChangeContactAsync(
        long contact,
        ChangeKind change,
        CancellationToken cancellationToken)
    {
        if (contact < 1)
        {
            return this.BadRequest();
        }

        ContactStatusRequest request = new ContactStatusRequest
        {
            Id = new ContactId(contact),
            Organization = new OrganizationId(this.Input.Id),
            ChangedBy = SignedInUser.Of(this.User),
        };
        UpdateOutcome outcome = change == ChangeKind.Archived
            ? await this._contacts.ArchiveAsync(request, cancellationToken).ConfigureAwait(false)
            : await this._contacts.RestoreAsync(request, cancellationToken).ConfigureAwait(false);
        OrganizationSingleResponse? organization = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return this.ShowGone();
        }

        // The list is drawn again with the keyboard on the row's menu button, where it was before the menu opened.
        this.Focused = request.Id;

        // Undo is offered only for a change that was made: it posts the opposite change, from the same page.
        string? undo = null;
        if (outcome == UpdateOutcome.Saved)
        {
            string opposite = change == ChangeKind.Archived ? RestoreContactHandler : ArchiveContactHandler;
            undo = this.ContactAddress(opposite, request.Id);
        }

        string message = this.ContactMessage(change, outcome, request.Id, organization.Name);
        return this.Answer(message, undo, ListChangedPartial, ListTarget);
    }

    /// <summary>
    /// Writes the toast's message for what became of an archive or a restore of a contact.
    /// </summary>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="outcome">What became of it.</param>
    /// <param name="contact">The contact.</param>
    /// <param name="organization">The organization's name.</param>
    /// <returns>The message.</returns>
    private string ContactMessage(
        ChangeKind change,
        UpdateOutcome outcome,
        ContactId contact,
        OrganizationName organization)
    {
        bool archiving = change == ChangeKind.Archived;
        string name = this.NameOf(contact);
        return outcome switch
        {
            UpdateOutcome.Saved =>
                archiving ? $"{name} archived." : $"{name} restored.",
            UpdateOutcome.Unchanged =>
                archiving ? $"{name} was already archived." : $"{name} was already active.",
            UpdateOutcome.Archived =>
                $"{organization.Value} is archived, so its contacts can't be changed until it is restored.",
            UpdateOutcome.NotFound =>
                $"That contact no longer belongs to {organization.Value}.",
            _ =>
                $"{name} was changed by someone else at the same moment. Try again.",
        };
    }

    /// <summary>
    /// Gives the name of a contact on the page of the list just read.
    /// </summary>
    /// <param name="contact">The contact.</param>
    /// <returns>The name, or "The contact" when they are not on the page.</returns>
    private string NameOf(ContactId contact)
    {
        ContactSummary[] shown = this.Contacts?.Items ?? [];
        ContactSummary? found = shown.FirstOrDefault(summary => summary.Id == contact);
        return found?.Name.Value ?? "The contact";
    }

    /// <summary>
    /// Where the list is, from the query string, as the browser sent it. Both values are optional; with neither, the
    /// list starts at its first page.
    /// </summary>
    public sealed class PagingInputModel
    {
        /// <summary>
        /// Gets or sets the identifier of the contact the page starts after, for Next.
        /// </summary>
        public long? After { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the contact the page ends before, for Previous.
        /// </summary>
        public long? Before { get; set; }
    }
}
