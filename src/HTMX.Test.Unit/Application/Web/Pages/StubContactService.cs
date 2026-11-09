namespace HTMX.Test.Unit.Application.Web.Pages;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Stands in for the contact service in page tests: it keeps each request the page sends, answers a page of contacts
/// with the contacts it was given as the page the request asked for, a lookup with the one contact and the versions it
/// was given, and an archive, a restore or a roll-back with the outcome it was given, so a test sees what the page
/// asked for without a database.
/// </summary>
internal sealed class StubContactService : IContactService
{
    private readonly List<ContactCollectionRequest> _received = [];
    private readonly List<(ChangeKind Change, ContactStatusRequest Request)> _statusChanges = [];
    private readonly List<ContactRollBackRequest> _rollBacks = [];
    private readonly ContactSummary[] _contacts;
    private readonly UpdateOutcome _outcome;
    private readonly ContactSingleResponse? _found;
    private readonly ContactVersionResponse[] _versions;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubContactService"/> class.
    /// </summary>
    /// <param name="contacts">The contacts every page holds.</param>
    /// <param name="outcome">What an archive, a restore and a roll-back are answered with.</param>
    /// <param name="found">The contact a lookup answers with, or <see langword="null"/> for none.</param>
    /// <param name="versions">The versions a lookup of the history answers with, newest first.</param>
    public StubContactService(
        ContactSummary[]? contacts = null,
        UpdateOutcome outcome = UpdateOutcome.Saved,
        ContactSingleResponse? found = null,
        ContactVersionResponse[]? versions = null)
    {
        this._contacts = contacts ?? [];
        this._outcome = outcome;
        this._found = found;
        this._versions = versions ?? [];
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

    /// <summary>
    /// Gives the last roll-back the page asked for.
    /// </summary>
    /// <returns>The roll-back, or <see langword="null"/> before the page asks for one.</returns>
    public ContactRollBackRequest? LastRollBack()
    {
        return this._rollBacks.LastOrDefault();
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
    public Task<ContactSingleResponse?> FindAsync(ContactId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this._found);
    }

    /// <inheritdoc/>
    public Task<ContactVersionResponse[]> ListVersionsAsync(
        ContactId id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this._versions);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> RollBackAsync(
        ContactRollBackRequest request,
        CancellationToken cancellationToken = default)
    {
        this._rollBacks.Add(request);
        return Task.FromResult(this._outcome);
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
