namespace HTMX.Application.Web.Authentication;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// The visitors' own sign-in: a cookie that signs a pretend visitor in for thirty seconds, with a pass or without one.
/// The Sign-in expiry page shows with it what a request meets once a sign-in has expired, and the Visitors' room what
/// a request meets when its visitor has no pass. It sits beside the Demo sign-in every other page uses.
/// </summary>
public static class VisitorSignIn
{
    /// <summary>
    /// The name the scheme is registered under, which a page or a policy that needs a visitor names.
    /// </summary>
    public const string SchemeName = "Visitor";

    /// <summary>
    /// The sign-in page's address.
    /// </summary>
    public const string SignInPath = "/components/visitor-sign-in";

    /// <summary>
    /// The access-denied page's address, where a visitor goes who may not see the page asked for.
    /// </summary>
    public const string AccessDeniedPath = "/components/visitor-access-denied";

    /// <summary>
    /// The claim that holds when the sign-in ends, as a round-trip date and time, so that the page can say so.
    /// </summary>
    public const string EndsClaim = "sign-in-ends";

    /// <summary>
    /// The claim a visitor signed in with a pass carries, which the Visitors' room asks for.
    /// </summary>
    public const string PassClaim = "visitor-pass";

    /// <summary>
    /// How long a sign-in lasts. It does not slide: using the page does not make it last longer.
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Sets the scheme's cookie options: its sign-in and access-denied pages, its lifetime, and the answers to a
    /// request that needs a visitor and has none, and to one whose visitor may not see the page, which differ for HTMX.
    /// </summary>
    /// <param name="options">The scheme's options.</param>
    public static void Configure(CookieAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.LoginPath = SignInPath;
        options.AccessDeniedPath = AccessDeniedPath;
        options.ExpireTimeSpan = Lifetime;
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = HtmxCookieEvents.RedirectToLogin;
        options.Events.OnRedirectToAccessDenied = HtmxCookieEvents.RedirectToAccessDenied;
    }

    /// <summary>
    /// Makes the visitor a sign-in signs in.
    /// </summary>
    /// <param name="ends">When the sign-in ends.</param>
    /// <param name="pass">Whether the visitor has a pass, which lets them into the Visitors' room.</param>
    /// <returns>The visitor: an identifier, a name, when the sign-in ends, and the pass if they have one.</returns>
    public static ClaimsPrincipal Visitor(DateTimeOffset ends, bool pass)
    {
        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, "visitor"),
            new Claim(ClaimTypes.Name, "Visitor"),
            new Claim(EndsClaim, ends.ToString("O", CultureInfo.InvariantCulture)),
        ];
        if (pass)
        {
            claims.Add(new Claim(PassClaim, "true"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
    }
}
