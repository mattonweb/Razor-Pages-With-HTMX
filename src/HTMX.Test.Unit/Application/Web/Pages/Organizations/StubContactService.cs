namespace HTMX.Test.Unit.Application.Web.Pages.Organizations;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Stands in for the contact service in page tests: it keeps each request the page sends, answers a page of contacts
/// with the contacts it was given as the page the request asked for, and answers an archive or a restore with the
/// outcome it was given, so a test sees what the page asked for without a database.
/// </summary>
internal sealed class StubContactService : IContactService
{
    private readonly List<ContactCollectionRequest> _received = [];
    private readonly List<(ChangeKind Change, ContactStatusRequest Request)> _statusChanges = [];
    private readonly ContactSummary[] _contacts;
    private readonly UpdateOutcome _outcome;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubContactService"/> class.
    /// </summary>
    /// <param name="contacts">The contacts every page holds.</param>
    /// <param name="outcome">What an archive and a restore are answered with.</param>
    public StubContactService(ContactSummary[]? contacts = null, UpdateOutcome outcome = UpdateOutcome.Saved)
    {
        this._contacts = contacts ?? [];
        this._outcome = outcome;
    }

    /// <summary>
    /// Gives the last page of contacts the page asked for.
    /// </summary>
    /// <returns>The request, or <see langword="null"/> before the page sends one.</returns>
    public ContactCollectionRequest? LastRequest()
    {
        return this._received.LastOrDefault();
    }

    /// <summary>
    /// Gives the last archive or restore the page asked for.
    /// </summary>
    /// <returns>Archived or Restored, with the request, or <see langword="null"/> before the page asks for
    /// either.</returns>
    public (ChangeKind Change, ContactStatusRequest Request)? LastStatusChange()
    {
        return this._statusChanges.Count == 0 ? null : this._statusChanges[^1];
    }

    /// <inheritdoc/>
    public Task<ContactCollectionResponse> ListAsync(
        ContactCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        this._received.Add(request);
        ContactCollectionResponse response = new ContactCollectionResponse
        {
            Request = request,
            Items = this._contacts,
            Total = new RecordCount(this._contacts.Length),
        };
        return Task.FromResult(response);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> ArchiveAsync(
        ContactStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        this._statusChanges.Add((ChangeKind.Archived, request));
        return Task.FromResult(this._outcome);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> RestoreAsync(
        ContactStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        this._statusChanges.Add((ChangeKind.Restored, request));
        return Task.FromResult(this._outcome);
    }
}
