namespace HTMX.Application.Web.Authorization;

using System.Collections.Generic;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Who may publish the Not allowed page's articles: Demo User at first, until the Publishing permission page withdraws
/// it. An application keeps such a grant with its users, in its database; one set for the whole application stands in
/// for that here.
/// </summary>
public sealed class Publishers
{
    // Demo User, as the seed writes them: README.md names the seed's users.
    private static readonly UserId DemoUser = new UserId(10001);

    // The users who may publish. Every read and write locks it.
    private readonly HashSet<UserId> _users = [DemoUser];

    /// <summary>
    /// Says whether a user may publish.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns><see langword="true"/> when the user may.</returns>
    public bool May(UserId user)
    {
        lock (this._users)
        {
            return this._users.Contains(user);
        }
    }

    /// <summary>
    /// Lets a user publish.
    /// </summary>
    /// <param name="user">The user.</param>
    public void Grant(UserId user)
    {
        lock (this._users)
        {
            this._users.Add(user);
        }
    }

    /// <summary>
    /// Stops a user publishing. A page the user already has open keeps what it drew until it is drawn again.
    /// </summary>
    /// <param name="user">The user.</param>
    public void Withdraw(UserId user)
    {
        lock (this._users)
        {
            this._users.Remove(user);
        }
    }
}
