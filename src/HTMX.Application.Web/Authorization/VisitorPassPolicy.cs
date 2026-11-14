namespace HTMX.Application.Web.Authorization;

using System;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// The Visitors' room's policy: a visitor signed in with a pass. It names the visitor's sign-in, so a request with no
/// visitor meets that sign-in's challenge, its sign-in page, and a visitor with no pass meets its forbid, its
/// access-denied page.
/// </summary>
public static class VisitorPassPolicy
{
    /// <summary>
    /// The name the policy is registered under, which a page asks for.
    /// </summary>
    public const string Name = "VisitorPass";

    /// <summary>
    /// Adds the policy to the application's authorization options.
    /// </summary>
    /// <param name="options">The options.</param>
    public static void Configure(AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.AddPolicy(Name, Build);
    }

    /// <summary>
    /// Builds the policy: a visitor, signed in with the visitor's sign-in, who carries a pass.
    /// </summary>
    /// <param name="policy">The policy being built.</param>
    private static void Build(AuthorizationPolicyBuilder policy)
    {
        policy.AddAuthenticationSchemes(VisitorSignIn.SchemeName);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(VisitorSignIn.PassClaim);
    }
}
