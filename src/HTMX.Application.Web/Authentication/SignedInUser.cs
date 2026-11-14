namespace HTMX.Application.Web.Authentication;

using System;
using System.Globalization;
using System.Security.Claims;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Reads who is signed in from a request's user, for a change that must say who made it, or for a permission that
/// depends on who it is.
/// </summary>
public static class SignedInUser
{
    /// <summary>
    /// Gives the signed-in user's identifier, from the name-identifier claim the sign-in scheme sets.
    /// </summary>
    /// <param name="user">The request's user, a page's <c>User</c>.</param>
    /// <returns>The identifier.</returns>
    /// <exception cref="InvalidOperationException">No user is signed in, or the claim is not a user's identifier:
    /// every request is signed in, so either means the application is misconfigured.</exception>
    public static UserId Of(ClaimsPrincipal user)
    {
        return Find(user)
            ?? throw new InvalidOperationException("The request has no signed-in user with a numeric identifier.");
    }

    /// <summary>
    /// Looks for the signed-in user's identifier, for a check that answers no rather than failing when there is none,
    /// as an authorization handler does.
    /// </summary>
    /// <param name="user">The request's user.</param>
    /// <returns>The identifier, or <see langword="null"/> when no user is signed in or the claim is not a user's
    /// identifier.</returns>
    public static UserId? Find(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        string? claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(claim, NumberStyles.None, CultureInfo.InvariantCulture, out long id))
        {
            return null;
        }

        return new UserId(id);
    }
}
