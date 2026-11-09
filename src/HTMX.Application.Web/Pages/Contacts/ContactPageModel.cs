namespace HTMX.Application.Web.Pages.Contacts;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// What every tab of a contact's page shares: the contact the route names, and their header with Archive or Restore.
/// The identifier, the toast and the answers an action gives are <see cref="RecordPageModel"/>'s.
/// </summary>
/// <remarks>
/// A contact is changed only while their organization is active, as on the organization's Contacts tab; while it is
/// archived the page shows the contact and says why nothing can be changed.
/// </remarks>
public abstract class ContactPageModel : RecordPageModel
{
    private const string ArchiveHandler = "Archive";
    private const string RestoreHandler = "Restore";

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactPageModel"/> class.
    /// </summary>
    /// <param name="contacts">Reads and changes the contact.</param>
    protected ContactPageModel(IContactService contacts)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        this.Contacts = contacts;
    }

    /// <summary>
    /// Gets the contact the page shows, or <see langword="null"/> when the identifier names none.
    /// </summary>
    public ContactSingleResponse? Contact { get; private set; }

    /// <summary>
    /// Gets the service the tab reads and changes the contact through.
    /// </summary>
    protected IContactService Contacts { get; }

    /// <inheritdoc/>
    protected override string ContentTarget => "#contact";

    /// <summary>
    /// Archives the contact, and says so in a toast whose Undo restores them.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise.</returns>
    public Task<IActionResult> OnPostArchiveAsync(CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(ChangeKind.Archived, cancellationToken);
    }

    /// <summary>
    /// Restores the contact, and says so in a toast whose Undo archives them again.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise.</returns>
    public Task<IActionResult> OnPostRestoreAsync(CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(ChangeKind.Restored, cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task<bool> FindRecordAsync(CancellationToken cancellationToken)
    {
        ContactSingleResponse? contact = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        return contact is not null;
    }

    /// <summary>
    /// Reads, after the contact, whatever else the tab shows of them. The Details tab needs nothing more.
    /// </summary>
    /// <param name="id">The contact's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>A task that completes when it is read.</returns>
    protected virtual Task ReadMoreAsync(ContactId id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads the contact the route names and, when there is one, what the tab shows of them.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The contact, or <see langword="null"/> when there is none.</returns>
    protected async Task<ContactSingleResponse?> ReadAsync(CancellationToken cancellationToken)
    {
        ContactId id = new ContactId(this.Input.Id);
        this.Contact = await this.Contacts.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (this.Contact is not null)
        {
            await this.ReadMoreAsync(id, cancellationToken).ConfigureAwait(false);
        }

        return this.Contact;
    }

    /// <summary>
    /// Goes to the contact's own page after an action found them gone, so that the page says so.
    /// </summary>
    /// <returns>A redirect, or 204 with the request to load the page.</returns>
    protected IActionResult ShowGone()
    {
        return HtmxAnswers.NextPage(this, "./Details", new { id = this.Input.Id });
    }

    /// <summary>
    /// Archives or restores the contact, reads them again, and answers with what they now are and a toast that says
    /// what happened. When the contact no longer exists, their page says so.
    /// </summary>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise.</returns>
    private async Task<IActionResult> ChangeStatusAsync(ChangeKind change, CancellationToken cancellationToken)
    {
        // Made on the contact's own page, so it names no organization: the contact is changed wherever they belong.
        ContactStatusRequest request = new ContactStatusRequest
        {
            Id = new ContactId(this.Input.Id),
            ChangedBy = SignedInUser.Of(this.User),
        };
        UpdateOutcome outcome = change == ChangeKind.Archived
            ? await this.Contacts.ArchiveAsync(request, cancellationToken).ConfigureAwait(false)
            : await this.Contacts.RestoreAsync(request, cancellationToken).ConfigureAwait(false);
        ContactSingleResponse? contact = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (contact is null)
        {
            return this.ShowGone();
        }

        // Undo is offered only for a change that was made: it posts the opposite change.
        string? undo = null;
        if (outcome == UpdateOutcome.Saved)
        {
            string opposite = change == ChangeKind.Archived ? RestoreHandler : ArchiveHandler;
            undo = this.HandlerAddress(opposite, this.TabValues());
        }

        string message = ContactMessages.Status(change, outcome, contact.Name.Value, contact.Organization.Name);
        return this.Answer(message, undo);
    }
}
