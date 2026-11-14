namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Sign-in expiry page, at /components/sign-in-expiry, run through the whole
/// application: a request with no visitor's sign-in, as after the sign-in has expired, goes to the sign-in page by a
/// redirect or, for HTMX, by HX-Redirect; and a sign-in goes back only to a page of this site.
/// </summary>
[TestClass]
public sealed class SignInExpiryTests : IDisposable
{
    #region Private Fields

    // The page, and its sign-in page with the page as the address to come back to, as the redirect names it.
    private const string Address = "/components/sign-in-expiry";
    private const string SignInPage = "/components/visitor-sign-in";
    private const string SignInForPage = "/components/visitor-sign-in?ReturnUrl=%2Fcomponents%2Fsign-in-expiry";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that the browser on its own, with no visitor's sign-in, is redirected to the sign-in
    /// page, which will come back to the page.
    /// </summary>
    [TestMethod]
    public async Task Get_NoSignInWithoutHtmx_RedirectsToSignIn()
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
            SignInForPage,
            answer.Headers["Location"],
            "The redirect should go to the sign-in page, which comes back to the page.");
    }

    /// <summary>
    /// Integration test to verify that HTMX asking for part of the page, with no visitor's sign-in, gets 204 and
    /// HX-Redirect naming the sign-in page, which comes back to the page HX-Current-URL names, and no redirect.
    /// </summary>
    [TestMethod]
    public async Task Get_NoSignInByHtmx_AsksForTheSignInPage()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        Dictionary<string, string> current = new Dictionary<string, string>
        {
            ["HX-Current-URL"] = "http://localhost/components/sign-in-expiry",
        };

        // Act (When)
        Answer answer = await Requests.GetAsync(browser, Address, HtmxRequest.Partial, current).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.NoContent, SignInForPage, false),
            (answer.Status, answer.Headers["HX-Redirect"], answer.Headers.ContainsKey("Location")),
            "HTMX should be asked to load the sign-in page, not redirected.");
    }

    /// <summary>
    /// Integration test to verify that a sign-in goes back to the page it names, which the visitor may then see.
    /// </summary>
    [TestMethod]
    public async Task Post_SignIn_GoesBackToThePage()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, SignInForPage).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, [Field("ReturnUrl", Address)]);

        // Act (When)
        Answer signedIn = await Requests.PostAsync(browser, SignInPage, HtmxRequest.None, form).ConfigureAwait(false);
        Answer page = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.Redirect, Address, HttpStatusCode.OK),
            (signedIn.Status, signedIn.Headers["Location"], page.Status),
            "The sign-in should go back to the page, which should then show.");
        Assert.Contains(
            "The sign-in ends at",
            page.Body,
            "The page should say when the sign-in ends.");
    }

    /// <summary>
    /// Integration test to verify that a sign-in told to go back to another site, or to an address the browser would
    /// read as another site's, goes to the Sign-in expiry page instead, with HTMX and without it.
    /// </summary>
    /// <param name="returnUrl">The address the sign-in is told to go back to.</param>
    /// <param name="kind">What sends the request, by its name in <see cref="HtmxRequest"/>.</param>
    [TestMethod]
    [DataRow("https://elsewhere.example/", nameof(HtmxRequest.None), DisplayName = "Another site, without HTMX")]
    [DataRow("//elsewhere.example/", nameof(HtmxRequest.None), DisplayName = "A protocol-relative address")]
    [DataRow("https://elsewhere.example/", nameof(HtmxRequest.Boosted), DisplayName = "Another site, with HTMX")]
    public async Task Post_SignInToAnotherSite_GoesToThePageInstead(string returnUrl, string kind)
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, SignInPage).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, [Field("ReturnUrl", returnUrl)]);

        // Act (When)
        HtmxRequest sender = Enum.Parse<HtmxRequest>(kind);
        Answer answer = await Requests.PostAsync(browser, SignInPage, sender, form).ConfigureAwait(false);
        string header = sender == HtmxRequest.None ? "Location" : "HX-Location";

        // Assert (Then)
        Assert.AreEqual(
            Address,
            answer.Headers[header],
            "The sign-in should go to the Sign-in expiry page, not to another site.");
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
    /// Makes one of a form's fields.
    /// </summary>
    /// <param name="name">The field's name.</param>
    /// <param name="value">Its value.</param>
    /// <returns>The field.</returns>
    private static KeyValuePair<string, string> Field(string name, string value)
    {
        return new KeyValuePair<string, string>(name, value);
    }

    #endregion Private Methods
}
