namespace HTMX.Adapter.Customers.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.DTOs;
using HTMX.Adapter.Customers.Providers;

/// <summary>
/// The rules over users, behind <see cref="IUserService"/>; the queries themselves are the provider's.
/// </summary>
internal sealed class UserService : IUserService
{
    private readonly UserProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="provider">Runs the queries over users.</param>
    public UserService(UserProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        this._provider = provider;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// As for the organizations list, a page is read one user longer than it shows: if that extra user is there, so is
    /// another page, and no count is needed to know it. A cursor whose user no longer exists, or that leaves too little
    /// to show, starts again at the first page, so an old link never shows an empty or a short page.
    /// </remarks>
    public async Task<UserCollectionResponse> ListAsync(
        UserCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RecordCount active = await this._provider
            .CountAsync(request.Filter, RecordStatus.Active, cancellationToken)
            .ConfigureAwait(false);
        RecordCount archived = await this._provider
            .CountAsync(request.Filter, RecordStatus.Archived, cancellationToken)
            .ConfigureAwait(false);

        UserSortKey? key = await this.FindCursorKeyAsync(request.Cursor, cancellationToken).ConfigureAwait(false);
        UserCollectionRequest answered = key is null ? UserPages.FirstPage(request) : request;
        UserSummary[] read = await this.ReadAsync(answered, key, cancellationToken).ConfigureAwait(false);
        if (key is not null && UserPages.IsTooShort(answered, read))
        {
            answered = UserPages.FirstPage(request);
            read = await this.ReadAsync(answered, null, cancellationToken).ConfigureAwait(false);
        }

        return UserPages.CreatePage(answered, read, active, archived);
    }

    /// <summary>
    /// Reads how the list sorts the user a cursor names.
    /// </summary>
    /// <param name="cursor">The cursor, if any.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the values.</param>
    /// <returns>The values, or <see langword="null"/> for no cursor or a user who no longer exists.</returns>
    private async Task<UserSortKey?> FindCursorKeyAsync(UserCursor? cursor, CancellationToken cancellationToken)
    {
        if (cursor is not UserCursor named)
        {
            return null;
        }

        return await this._provider.FindSortKeyAsync(named.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads one user more than a page holds, starting just past the cursor's user.
    /// </summary>
    /// <param name="request">The page to read.</param>
    /// <param name="key">How the list sorts the cursor's user, or <see langword="null"/> for the first page.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the users.</param>
    /// <returns>The users, in the order they were read.</returns>
    private Task<UserSummary[]> ReadAsync(
        UserCollectionRequest request,
        UserSortKey? key,
        CancellationToken cancellationToken)
    {
        RecordCount limit = new RecordCount(request.PageSize.Value + 1);
        return this._provider.ListAsync(request, key, limit, cancellationToken);
    }
}
