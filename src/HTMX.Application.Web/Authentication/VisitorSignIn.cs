namespace HTMX.Application.Web.Authentication;

using System;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// The Sign-in expiry page's own sign-in: a cookie that signs a pretend visitor in for thirty seconds, so that the page
/// can show what a request meets once a sign-in has expired. It sits beside the Demo sign-in every other page uses.
/// </summary>
public static class VisitorSignIn
{
    /// <summary>
    /// The name the scheme is registered under, which the page that needs a visitor names.
    /// </summary>
    public const string SchemeName = "Visitor";

    /// <summary>
    /// The sign-in page's address.
    /// </summary>
    public const string SignInPath = "/components/visitor-sign-in";

    /// <summary>
    /// The claim that holds when the sign-in ends, as a round-trip date and time, so that the page can say so.
    /// </summary>
    public const string EndsClaim = "sign-in-ends";

    /// <summary>
    /// How long a sign-in lasts. It does not slide: using the page does not make it last longer.
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Sets the scheme's cookie options: its sign-in page, its lifetime, and the answer to a request that needs a
    /// visitor and has none, which differs for HTMX.
    /// </summary>
    /// <param name="options">The scheme's options.</param>
    public static void Configure(CookieAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.LoginPath = SignInPath;
        options.ExpireTimeSpan = Lifetime;
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = HtmxCookieEvents.RedirectToLogin;
    }

    /// <summary>
    /// Makes the visitor a sign-in signs in.
    /// </summary>
    /// <param name="ends">When the sign-in ends.</param>
    /// <returns>The visitor, with an identifier, a name and when the sign-in ends.</returns>
    public static ClaimsPrincipal Visitor(DateTimeOffset ends)
    {
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, "visitor"),
            new Claim(ClaimTypes.Name, "Visitor"),
            new Claim(EndsClaim, ends.ToString("O", CultureInfo.InvariantCulture)),
        ];
        return new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
    }
}
