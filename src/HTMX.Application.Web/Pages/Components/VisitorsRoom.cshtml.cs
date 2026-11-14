namespace HTMX.Application.Web.Pages.Components;

using System;
using HTMX.Application.Web.Authentication;
using HTMX.Application.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Visitors' room, at /components/visitors-room: a page only a visitor with a pass may see, whose card asks the
/// server for the time with HTMX.
/// </summary>
/// <remarks>
/// The page asks for <see cref="VisitorPassPolicy"/>, which names the visitor's sign-in, so the refusal comes before
/// the handler runs. A visitor who signed in again without a pass, in another tab, still sees the room as it was
/// drawn, and its next request meets the sign-in's forbid: the browser on its own is redirected to the access-denied
/// page, and HTMX is asked to load it with a reload (<see cref="HtmxCookieEvents"/>), rather than getting it as the
/// card's answer.
/// </remarks>
[Authorize(Policy = VisitorPassPolicy.Name)]
public sealed class VisitorsRoomModel : PageModel
{
    // The card that says the time, which the page draws and its Check again answers alone.
    private const string CardPartial = "_RoomCard";

    private readonly TimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisitorsRoomModel"/> class.
    /// </summary>
    /// <param name="clock">The server's clock.</param>
    public VisitorsRoomModel(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        this._clock = clock;
    }

    /// <summary>
    /// Gets the server's time, in its own time zone.
    /// </summary>
    public DateTimeOffset Now { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Shows the room, or, when HTMX asks for part of it, the card alone: Check again asking for the time.
    /// </summary>
    /// <returns>The card or the page.</returns>
    public IActionResult OnGet()
    {
        this.Now = this._clock.GetLocalNow();
        if (HtmxHeaders.IsPartialRequest(this.Request))
        {
            return this.Partial(CardPartial, this);
        }

        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        return this.Page();
    }
}
