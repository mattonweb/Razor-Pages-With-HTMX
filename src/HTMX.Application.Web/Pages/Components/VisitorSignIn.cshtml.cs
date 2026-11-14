namespace HTMX.Application.Web.Pages.Components;

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The visitor's sign-in page, at /components/visitor-sign-in: two buttons that sign a pretend visitor in for thirty
/// seconds, with no name or password, one of them with a pass, and go back to the page that asked for a sign-in.
/// </summary>
public sealed class VisitorSignInModel : PageModel
{
    // Where a sign-in goes when it was not sent from a page of this site.
    private const string DefaultPage = "/components/sign-in-expiry";

    private readonly TimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisitorSignInModel"/> class.
    /// </summary>
    /// <param name="clock">The server's clock, which says when the sign-in ends.</param>
    public VisitorSignInModel(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        this._clock = clock;
    }

    /// <summary>
    /// Gets or sets the page to go back to once signed in, as the sign-in's redirect named it. It is the sender's to
    /// write, so the page goes there only when it is an address on this site.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the visitor signs in with a pass: posted by the button that says so, as
    /// its name and value, and missing from a post by the other.
    /// </summary>
    [BindProperty]
    public bool Pass { get; set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Shows the sign-in.
    /// </summary>
    public void OnGet()
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
    }

    /// <summary>
    /// Signs the visitor in, until thirty seconds from now, with a pass when that button was pressed, in place of any
    /// visitor signed in before, and goes back to the page that asked: by a redirect without HTMX, and with HTMX by
    /// asking it to load that page.
    /// </summary>
    /// <returns>The redirect, or 204 with the page to load.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        DateTimeOffset ends = this._clock.GetUtcNow() + VisitorSignIn.Lifetime;
        AuthenticationProperties properties = new AuthenticationProperties { ExpiresUtc = ends };
        ClaimsPrincipal visitor = VisitorSignIn.Visitor(ends, this.Pass);
        await this.HttpContext.SignInAsync(VisitorSignIn.SchemeName, visitor, properties).ConfigureAwait(false);
        string? returnUrl = this.ReturnUrl;
        string next = returnUrl is not null && this.Url.IsLocalUrl(returnUrl) ? returnUrl : DefaultPage;
        if (!HtmxHeaders.IsHtmxRequest(this.Request))
        {
            return this.LocalRedirect(next);
        }

        HtmxHeaders.LoadPage(this.Response, next);
        return this.StatusCode(StatusCodes.Status204NoContent);
    }
}
