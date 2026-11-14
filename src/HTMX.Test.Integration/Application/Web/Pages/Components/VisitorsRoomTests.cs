namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Visitors' room, at /components/visitors-room, run through the whole application:
/// a visitor with a pass sees the room, and a visitor without one, as after signing in again without a pass, is sent
/// to the access-denied page by a redirect or, for HTMX, by HX-Redirect.
/// </summary>
[TestClass]
public sealed class VisitorsRoomTests : IDisposable
{
    #region Private Fields

    // The room, the visitor's sign-in, and the access-denied page with the room as the address to come back to.
    private const string Address = "/components/visitors-room";
    private const string SignInPage = "/components/visitor-sign-in";
    private const string DeniedForRoom = "/components/visitor-access-denied?ReturnUrl=%2Fcomponents%2Fvisitors-room";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that the browser on its own, with no visitor's sign-in, is redirected to the sign-in
    /// page, which will come back to the room: the policy names the visitor's sign-in, whose challenge it is.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task Get_NoVisitor_RedirectsToSignIn()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer answer = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            HttpStatusCode.Redirect,
            answer.Status,
            "The browser should be redirected.");
        Assert.EndsWith(
            "/components/visitor-sign-in?ReturnUrl=%2Fcomponents%2Fvisitors-room",
            answer.Headers["Location"],
            "The redirect should go to the sign-in page, which comes back to the room.");
    }

    /// <summary>
    /// Integration test to verify that a visitor signed in with a pass, by the button that posts it, sees the room.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task Get_VisitorWithAPass_ShowsTheRoom()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        await SignInAsync(browser, pass: true).ConfigureAwait(false);

        // Act (When)
        Answer answer = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            HttpStatusCode.OK,
            answer.Status,
            "A visitor with a pass should see the room.");
        Assert.Contains(
            "In the room with a pass",
            answer.Body,
            "The room should draw its card.");
    }

    /// <summary>
    /// Integration test to verify that the browser on its own, whose visitor has no pass, is redirected to the
    /// access-denied page, which comes back to the room.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task Get_VisitorWithoutAPassWithoutHtmx_RedirectsToAccessDenied()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        await SignInAsync(browser, pass: false).ConfigureAwait(false);

        // Act (When)
        Answer answer = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            HttpStatusCode.Redirect,
            answer.Status,
            "The browser should be redirected.");
        Assert.EndsWith(
            DeniedForRoom,
            answer.Headers["Location"],
            "The redirect should go to the access-denied page, which comes back to the room.");
    }

    /// <summary>
    /// Integration test to verify that HTMX asking for part of the room, whose visitor has no pass, gets 204 and
    /// HX-Redirect naming the access-denied page, which comes back to the page HX-Current-URL names, and no redirect,
    /// which <c>fetch</c> would follow and HTMX draw inside the card.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task Get_VisitorWithoutAPassByHtmx_AsksForTheAccessDeniedPage()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        await SignInAsync(browser, pass: false).ConfigureAwait(false);
        Dictionary<string, string> current = new Dictionary<string, string>
        {
            ["HX-Current-URL"] = "http://localhost/components/visitors-room",
        };

        // Act (When)
        Answer answer = await Requests.GetAsync(browser, Address, HtmxRequest.Partial, current).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.NoContent, DeniedForRoom, false),
            (answer.Status, answer.Headers["HX-Redirect"], answer.Headers.ContainsKey("Location")),
            "HTMX should be asked to load the access-denied page, not redirected.");
    }

    /// <summary>
    /// Browser test to verify, in Edge, that a visitor who signed in with a pass and then signed in again without one,
    /// in another tab, is sent from the room as it was drawn to the access-denied page by its Check again, which comes
    /// back to the room: with HTMX, by HX-Redirect and a reload, and with script off, by the redirect. The console
    /// reports no error.
    /// </summary>
    /// <param name="scripting">How much script the page runs, by its name in <see cref="Scripting"/>.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    [DataRow(nameof(Scripting.On), DisplayName = "With HTMX")]
    [DataRow(nameof(Scripting.Off), DisplayName = "With script off")]
    public async Task CheckAgain_AfterSigningInWithoutAPass_LoadsTheAccessDeniedPage(string scripting)
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Enum.Parse<Scripting>(scripting))
            .ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage room = edge.Page;
            await room.GotoAsync(Address).ConfigureAwait(false);
            await PressAsync(room, "Sign in with a pass").ConfigureAwait(false);
            await room.WaitForURLAsync("**" + Address).ConfigureAwait(false);
            IPage other = await room.Context.NewPageAsync().ConfigureAwait(false);
            await other.GotoAsync(SignInPage).ConfigureAwait(false);
            await PressAsync(other, "Sign in").ConfigureAwait(false);
            await other.WaitForURLAsync("**/components/sign-in-expiry").ConfigureAwait(false);

            // Act (When)
            await room.Locator("#room-check").ClickAsync().ConfigureAwait(false);
            await room.WaitForURLAsync("**" + DeniedForRoom).ConfigureAwait(false);
            string? heading = await room.Locator("h1").TextContentAsync().ConfigureAwait(false);
            int cards = await room.Locator("#room-card").CountAsync().ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                ("Access denied", 0),
                (heading?.Trim(), cards),
                "The access-denied page should be the whole page, not drawn inside the room's card.");
            Assert.IsEmpty(
                edge.ConsoleErrors,
                "The console should report no error: " + string.Join(" | ", edge.ConsoleErrors));
        }
    }

    /// <summary>
    /// Stops the application and deletes its database.
    /// </summary>
    public void Dispose()
    {
        this._application.Dispose();
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Signs a visitor in through the sign-in page's form, with a pass by the button that posts one, or without.
    /// </summary>
    /// <param name="browser">The client, which keeps the sign-in's cookie as a browser does.</param>
    /// <param name="pass">Whether the visitor signs in with a pass.</param>
    /// <returns>A task that completes when the visitor is signed in.</returns>
    private static async Task SignInAsync(HttpClient browser, bool pass)
    {
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, SignInPage).ConfigureAwait(false);
        KeyValuePair<string, string>[] button = pass ? [new KeyValuePair<string, string>("Pass", "true")] : [];
        await Requests.PostAsync(browser, SignInPage, HtmxRequest.None, Html.Form(token, button)).ConfigureAwait(false);
    }

    /// <summary>
    /// Presses a page's button by its exact name.
    /// </summary>
    /// <param name="page">The page.</param>
    /// <param name="name">The button's name.</param>
    /// <returns>A task that completes when the button has been pressed.</returns>
    private static Task PressAsync(IPage page, string name)
    {
        PageGetByRoleOptions exactly = new PageGetByRoleOptions { Name = name, Exact = true };
        return page.GetByRole(AriaRole.Button, exactly).ClickAsync();
    }

    #endregion Private Methods
}
