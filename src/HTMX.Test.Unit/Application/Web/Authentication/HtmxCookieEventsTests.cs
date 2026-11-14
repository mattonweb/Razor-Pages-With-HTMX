namespace HTMX.Test.Unit.Application.Web.Authentication;

using System;
using System.Threading.Tasks;
using HTMX.Application.Web.Authentication;
using HTMX.Test.Unit.Application.Web.Pages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="HtmxCookieEvents"/> class: what a cookie sign-in
/// answers a request that needs a signed-in user and has none, for the browser on its own and for HTMX.
/// </summary>
[TestClass]
public sealed class HtmxCookieEventsTests
{
    #region Private Fields

    // The sign-in page, and the address the cookie sign-in would redirect the browser to, as it builds it.
    private const string SignInPath = "/components/visitor-sign-in";
    private const string RedirectUri = "http://localhost/components/visitor-sign-in?ReturnUrl=%2Fcomponents%2Fstock";

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Unit test to verify that the browser on its own is redirected to the sign-in page, as the cookie sign-in would.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task RedirectToLogin_BrowserOnItsOwn_Redirects()
    {
        // Arrange (Given)
        RedirectContext<CookieAuthenticationOptions> context = CreateContext(HtmxRequest.None, "/components/stock");

        // Act (When)
        await HtmxCookieEvents.RedirectToLogin(context).ConfigureAwait(false);

        // Assert (Then)
        HttpResponse response = context.Response;
        Assert.AreEqual(
            (StatusCodes.Status302Found, RedirectUri),
            (response.StatusCode, response.Headers.Location.ToString()),
            "The browser should be redirected to the sign-in page.");
    }

    /// <summary>
    /// Unit test to verify that HTMX asking for part of a page gets 204 and HX-Redirect, with the page the user was on,
    /// from HX-Current-URL, as the address to come back to, rather than the part's own address.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task RedirectToLogin_PartByHtmx_ReturnsToTheCurrentPage()
    {
        // Arrange (Given)
        RedirectContext<CookieAuthenticationOptions> context = CreateContext(HtmxRequest.Partial, "/components/clock");
        context.Request.Headers["HX-Current-URL"] = "http://localhost/components/stock?tab=2";

        // Act (When)
        await HtmxCookieEvents.RedirectToLogin(context).ConfigureAwait(false);

        // Assert (Then)
        HttpResponse response = context.Response;
        Assert.AreEqual(
            (StatusCodes.Status204NoContent, "/components/visitor-sign-in?ReturnUrl=%2Fcomponents%2Fstock%3Ftab%3D2"),
            (response.StatusCode, response.Headers["HX-Redirect"].ToString()),
            "HTMX should be asked to load the sign-in page, coming back to the page the user was on.");
    }

    /// <summary>
    /// Unit test to verify that HTMX asking for a whole page, as a boosted link or Back does, gets 204 and HX-Redirect,
    /// with that page as the address to come back to: the page the user was on is the one they are leaving.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Boosted), DisplayName = "A boosted link")]
    [DataRow(nameof(HtmxRequest.HistoryRestore), DisplayName = "Back")]
    public async Task RedirectToLogin_WholePageByHtmx_ReturnsToThatPage(string kind)
    {
        // Arrange (Given)
        HtmxRequest sender = Enum.Parse<HtmxRequest>(kind);
        RedirectContext<CookieAuthenticationOptions> context = CreateContext(sender, "/components/stock");
        context.Request.Headers["HX-Current-URL"] = "http://localhost/components";

        // Act (When)
        await HtmxCookieEvents.RedirectToLogin(context).ConfigureAwait(false);

        // Assert (Then)
        HttpResponse response = context.Response;
        Assert.AreEqual(
            (StatusCodes.Status204NoContent, "/components/visitor-sign-in?ReturnUrl=%2Fcomponents%2Fstock"),
            (response.StatusCode, response.Headers["HX-Redirect"].ToString()),
            "HTMX should be asked to load the sign-in page, coming back to the page it asked for.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the cookie sign-in's redirect for one request, as the sign-in builds it when the request has no user.
    /// </summary>
    /// <param name="kind">What sent the request.</param>
    /// <param name="path">The request's path.</param>
    /// <returns>The redirect's context.</returns>
    private static RedirectContext<CookieAuthenticationOptions> CreateContext(HtmxRequest kind, string path)
    {
        HttpContext http = PageRequests.Create(kind, "/Components/Index").HttpContext;
        http.Request.Path = path;
        AuthenticationScheme scheme = new AuthenticationScheme("Visitor", null, typeof(CookieAuthenticationHandler));
        CookieAuthenticationOptions options = new CookieAuthenticationOptions { LoginPath = SignInPath };
        return new RedirectContext<CookieAuthenticationOptions>(
            http,
            scheme,
            options,
            new AuthenticationProperties(),
            RedirectUri);
    }

    #endregion Private Methods
}
