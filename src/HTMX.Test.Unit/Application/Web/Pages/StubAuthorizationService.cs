namespace HTMX.Test.Unit.Application.Web.Pages;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Stands in for the authorization service in tests: it meets the policies it is given, by name, for any user, and no
/// other policy and no requirement asked for on its own.
/// </summary>
internal sealed class StubAuthorizationService : IAuthorizationService
{
    private readonly string[] _met;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubAuthorizationService"/> class.
    /// </summary>
    /// <param name="met">The names of the policies it meets: none, for a user no policy lets through.</param>
    public StubAuthorizationService(string[] met)
    {
        this._met = met;
    }

    /// <inheritdoc/>
    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        IEnumerable<IAuthorizationRequirement> requirements)
    {
        return Task.FromResult(AuthorizationResult.Failed());
    }

    /// <inheritdoc/>
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
    {
        bool met = Array.IndexOf(this._met, policyName) >= 0;
        return Task.FromResult(met ? AuthorizationResult.Success() : AuthorizationResult.Failed());
    }
}
