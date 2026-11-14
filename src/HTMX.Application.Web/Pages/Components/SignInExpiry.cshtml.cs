namespace HTMX.Application.Web.Pages.Components;

using System;
using System.Globalization;
using System.Security.Claims;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Sign-in expiry page, at /components/sign-in-expiry: a page only a signed-in visitor may see, whose card asks
/// the server for the time with HTMX, until the thirty-second sign-in has expired.
/// </summary>
/// <remarks>
/// The visitor's sign-in is a cookie scheme of its own (<see cref="VisitorSignIn"/>). Once it has expired, a request
/// for the page meets the sign-in's challenge: the browser on its own is redirected to the sign-in page, and HTMX is
/// asked to load it with a reload (<see cref="HtmxCookieEvents"/>), rather than getting it as the card's answer.
/// </remarks>
[Authorize(AuthenticationSchemes = VisitorSignIn.SchemeName)]
public sealed class SignInExpiryModel : PageModel
{
    // The card that says the time, which the page draws and its Check again answers alone.
    private const string ClockPartial = "_SignInClock";

    private readonly TimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignInExpiryModel"/> class.
    /// </summary>
    /// <param name="clock">The server's clock.</param>
    public SignInExpiryModel(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        this._clock = clock;
    }

    /// <summary>
    /// Gets the server's time, in its own time zone.
    /// </summary>
    public DateTimeOffset Now { get; private set; }

    /// <summary>
    /// Gets when the visitor's sign-in ends, in the server's time zone, or <see langword="null"/> when the sign-in does
    /// not say.
    /// </summary>
    public DateTimeOffset? Ends { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Shows the page, or, when HTMX asks for part of it, the card alone: Check again asking for the time.
    /// </summary>
    /// <returns>The card or the page.</returns>
    public IActionResult OnGet()
    {
        this.Now = this._clock.GetLocalNow();
        this.Ends = this.SignInEnds();
        if (HtmxHeaders.IsPartialRequest(this.Request))
        {
            return this.Partial(ClockPartial, this);
        }

        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        return this.Page();
    }

    /// <summary>
    /// Reads when the visitor's sign-in ends from the claim the sign-in wrote.
    /// </summary>
    /// <returns>The time, in the server's time zone, or <see langword="null"/>.</returns>
    private DateTimeOffset? SignInEnds()
    {
        string? claim = this.User.FindFirstValue(VisitorSignIn.EndsClaim);
        CultureInfo invariant = CultureInfo.InvariantCulture;
        if (!DateTimeOffset.TryParse(claim, invariant, DateTimeStyles.RoundtripKind, out DateTimeOffset ends))
        {
            return null;
        }

        return TimeZoneInfo.ConvertTime(ends, this._clock.LocalTimeZone);
    }
}
