namespace HTMX.Application.Web.Pages.Contacts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// The History tab of a contact's page, at /contacts/{id}/history: every version, newest first, with what each
/// changed, and a roll-back to any earlier one whose organization still takes people in. Rolling back saves the
/// earlier values as a new version, so it says so in a toast whose Undo rolls back again, to the version it replaced.
/// </summary>
public sealed class HistoryModel : ContactPageModel
{
    private const string RollBackHandler = "RollBack";

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryModel"/> class.
    /// </summary>
    /// <param name="contacts">Reads the contact and their versions, and rolls them back.</param>
    public HistoryModel(IContactService contacts)
        : base(contacts)
    {
    }

    /// <inheritdoc/>
    public override string PageName => "./History";

    /// <inheritdoc/>
    public override string ContentPartial => "_ContactHistory";

    /// <summary>
    /// Gets the contact's versions, newest first.
    /// </summary>
    public IReadOnlyList<ContactVersionResponse> Versions { get; private set; } = [];

    /// <summary>
    /// Gets the versions as the timeline shows them, or <see langword="null"/> until they are read.
    /// </summary>
    public Timeline? Timeline { get; private set; }

    /// <summary>
    /// Rolls the contact back to an earlier version, from the version the page showed them at.
    /// </summary>
    /// <param name="to">The version to roll back to, from the preview's form.</param>
    /// <param name="version">The version the page showed the contact at, from the preview's form.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise; 400 for a version
    /// number that cannot be one.</returns>
    public async Task<IActionResult> OnPostRollBackAsync(int to, int version, CancellationToken cancellationToken)
    {
        if (to < 1 || version < 1)
        {
            return this.BadRequest();
        }

        ContactRollBackRequest request = new ContactRollBackRequest
        {
            Id = new ContactId(this.Input.Id),
            Version = new VersionNumber(version),
            To = new VersionNumber(to),
            ChangedBy = SignedInUser.Of(this.User),
        };
        UpdateOutcome outcome = await this.Contacts.RollBackAsync(request, cancellationToken).ConfigureAwait(false);
        ContactSingleResponse? contact = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (contact is null)
        {
            return this.ShowGone();
        }

        // Undo rolls back again, to the version this one replaced, from the version the roll-back made.
        string? undo = outcome == UpdateOutcome.Saved
            ? this.HandlerAddress(RollBackHandler, new { id = this.Input.Id, to = version, version = version + 1 })
            : null;
        return this.Answer(this.RollBackMessage(outcome, to, contact), undo);
    }

    /// <inheritdoc/>
    protected override async Task ReadMoreAsync(ContactId id, CancellationToken cancellationToken)
    {
        this.Versions = await this.Contacts.ListVersionsAsync(id, cancellationToken).ConfigureAwait(false);
        ContactSingleResponse contact = this.Contact
            ?? throw new InvalidOperationException("The versions are read after the contact.");
        this.Timeline = new Timeline
        {
            RecordName = contact.Name.Value,
            Current = contact.Version,
            Entries = ContactChanges.Entries(contact, this.Versions),
            RollBackAction = this.HandlerAddress(RollBackHandler, new { id = this.Input.Id }),
            Target = this.ContentTarget,
        };
    }

    /// <summary>
    /// Says why an archived contact, or a contact of an archived organization, cannot be rolled back.
    /// </summary>
    /// <param name="contact">The contact as they are now.</param>
    /// <returns>The message.</returns>
    private static string ClosedMessage(ContactSingleResponse contact)
    {
        string name = contact.Name.Value;
        return contact.Archived is null
            ? $"{contact.Organization.Name.Value} is archived, so its contacts can't be changed until it is restored."
            : $"{name} is archived, so they can't be rolled back until they are restored.";
    }

    /// <summary>
    /// Writes the toast's message for what became of a roll-back.
    /// </summary>
    /// <param name="outcome">What became of it.</param>
    /// <param name="to">The version it was to.</param>
    /// <param name="contact">The contact as they are now.</param>
    /// <returns>The message.</returns>
    private string RollBackMessage(UpdateOutcome outcome, int to, ContactSingleResponse contact)
    {
        string name = contact.Name.Value;
        return outcome switch
        {
            UpdateOutcome.Saved =>
                $"{name} rolled back to version {to}.",
            UpdateOutcome.Unchanged =>
                $"{name} already holds the values of version {to}.",
            UpdateOutcome.Archived =>
                ClosedMessage(contact),
            UpdateOutcome.OrganizationUnavailable =>
                this.UnavailableMessage(to, name),
            UpdateOutcome.NotFound =>
                $"{name} has no version {to}.",
            _ =>
                $"{name} has changed since their history was shown. Check the newest version, then try again.",
        };
    }

    /// <summary>
    /// Says why the contact cannot go back to a version whose organization takes no one in, in the timeline's words.
    /// </summary>
    /// <param name="number">The version's number.</param>
    /// <param name="name">The contact's name now.</param>
    /// <returns>The message.</returns>
    private string UnavailableMessage(int number, string name)
    {
        ContactVersionResponse? version = this.Versions.FirstOrDefault(version => version.Version.Value == number);
        return version is null
            ? $"The organization of version {number} takes no one in, so {name} can't go back to it."
            : ContactChanges.Unavailable(version, name);
    }
}
