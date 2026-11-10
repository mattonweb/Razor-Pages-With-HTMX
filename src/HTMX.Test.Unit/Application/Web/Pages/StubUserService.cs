namespace HTMX.Test.Unit.Application.Web.Pages;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Stands in for the user service in page tests: it keeps each request the page sends and answers with an empty page
/// for a list, with the one user and the versions it was given for a lookup, and with the outcome it was given for an
/// archive, a restore or a roll-back, so a test sees what the page asked for without a database.
/// </summary>
internal sealed class StubUserService : IUserService
{
    private readonly List<UserCollectionRequest> _received = [];
    private readonly List<UserId> _looked = [];
    private readonly List<(ChangeKind Change, UserStatusRequest Request)> _statusChanges = [];
    private readonly List<UserRollBackRequest> _rollBacks = [];
    private readonly UserSingleResponse? _found;
    private readonly UpdateOutcome _outcome;
    private readonly UserVersionResponse[] _versions;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubUserService"/> class.
    /// </summary>
    /// <param name="found">The user a lookup answers with, or <see langword="null"/> for none.</param>
    /// <param name="outcome">What an archive, a restore and a roll-back are answered with.</param>
    /// <param name="versions">The versions a lookup of the history answers with, newest first.</param>
    public StubUserService(
        UserSingleResponse? found = null,
        UpdateOutcome outcome = UpdateOutcome.Saved,
        UserVersionResponse[]? versions = null)
    {
        this._found = found;
        this._outcome = outcome;
        this._versions = versions ?? [];
    }

    /// <summary>
    /// Gives the last request the page sent.
    /// </summary>
    /// <returns>The request, or <see langword="null"/> before the page sends one.</returns>
    public UserCollectionRequest? LastRequest()
    {
        return this._received.LastOrDefault();
    }

    /// <summary>
    /// Gives the identifier of the last user the page looked up.
    /// </summary>
    /// <returns>The identifier, or <see langword="null"/> before the page looks one up.</returns>
    public UserId? LastLookup()
    {
        return this._looked.Count == 0 ? null : this._looked[^1];
    }

    /// <summary>
    /// Gives the last archive or restore the page asked for.
    /// </summary>
    /// <returns>Archived or Restored, with the request, or <see langword="null"/> before the page asks for
    /// either.</returns>
    public (ChangeKind Change, UserStatusRequest Request)? LastStatusChange()
    {
        return this._statusChanges.Count == 0 ? null : this._statusChanges[^1];
    }

    /// <summary>
    /// Gives the last roll-back the page asked for.
    /// </summary>
    /// <returns>The roll-back, or <see langword="null"/> before the page asks for one.</returns>
    public UserRollBackRequest? LastRollBack()
    {
        return this._rollBacks.LastOrDefault();
    }

    /// <inheritdoc/>
    public Task<UserCollectionResponse> ListAsync(
        UserCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        this._received.Add(request);
        UserCollectionResponse response = new UserCollectionResponse
        {
            Request = request,
            Items = [],
            Matching = new RecordCount(0),
            Active = new RecordCount(0),
            Archived = new RecordCount(0),
            All = new RecordCount(0),
        };
        return Task.FromResult(response);
    }

    /// <inheritdoc/>
    public Task<UserSingleResponse?> FindAsync(UserId id, CancellationToken cancellationToken = default)
    {
        this._looked.Add(id);
        return Task.FromResult(this._found);
    }

    /// <inheritdoc/>
    public Task<UserVersionResponse[]> ListVersionsAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this._versions);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> RollBackAsync(
        UserRollBackRequest request,
        CancellationToken cancellationToken = default)
    {
        this._rollBacks.Add(request);
        return Task.FromResult(this._outcome);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> ArchiveAsync(UserStatusRequest request, CancellationToken cancellationToken = default)
    {
        this._statusChanges.Add((ChangeKind.Archived, request));
        return Task.FromResult(this._outcome);
    }

    /// <inheritdoc/>
    public Task<UpdateOutcome> RestoreAsync(UserStatusRequest request, CancellationToken cancellationToken = default)
    {
        this._statusChanges.Add((ChangeKind.Restored, request));
        return Task.FromResult(this._outcome);
    }
}
