namespace HTMX.Test.Integration.Application.Web.Pages.Organizations;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the organizations list, at /organizations, run through the whole application:
/// which answer each kind of request gets, and what an archive from a row answers, antiforgery check included.
/// </summary>
[TestClass]
public sealed class IndexTests : IDisposable
{
    #region Private Fields

    // A row's archive for Placeholder Logistics, posted to the list's own address as the row's form posts it.
    private const string ArchiveAddress = "/organizations?organization=10003&handler=Archive";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that HTMX swapping the card gets the card alone and anything else gets the whole
    /// page; that a page HTMX loaded, by a boosted link or by Back without HX-Request as HTMX 4.0.1 sends it, takes
    /// the keyboard to its heading; and that every answer says it varies by the header that chose it.
    /// </summary>
    /// <param name="kind">What sends the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="wholePage">Whether the answer should be the whole page.</param>
    /// <param name="headingFocused">Whether the page's heading should take the keyboard.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.None), true, false, DisplayName = "The browser on its own")]
    [DataRow(nameof(HtmxRequest.Partial), false, false, DisplayName = "HTMX swapping the card")]
    [DataRow(nameof(HtmxRequest.Boosted), true, true, DisplayName = "A boosted link")]
    [DataRow(nameof(HtmxRequest.HistoryRestore), true, true, DisplayName = "Back, as HTMX 4.0.1 sends it")]
    public async Task Get_EachKindOfRequest_AnswersTheCardOrThePage(string kind, bool wholePage, bool headingFocused)
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer answer = await Requests.GetAsync(browser, "/organizations", Enum.Parse<HtmxRequest>(kind))
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, wholePage, headingFocused),
            (answer.Status, Html.IsWholePage(answer.Body), Html.HeadingFocused(answer.Body)),
            "The request should get the card or the page, with the keyboard on the heading only when HTMX loaded it.");
        Assert.AreEqual(
            "HX-Request-Type",
            answer.Headers["Vary"],
            "Every answer should say that it varies by HX-Request-Type, so a cache keeps the two apart.");
    }

    /// <summary>
    /// Integration test to verify that an archive HTMX sends from a row passes the antiforgery check with the page's
    /// token and cookie, and answers with the list's card, the toast and the list's address for the address bar.
    /// </summary>
    [TestMethod]
    public async Task PostArchive_ByHtmx_AnswersTheCardAndAToast()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, "/organizations").ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, new Dictionary<string, string>());

        // Act (When)
        Answer answer = await Requests.PostAsync(browser, ArchiveAddress, HtmxRequest.Partial, form)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, false, "/organizations"),
            (answer.Status, Html.IsWholePage(answer.Body), answer.Headers["HX-Replace-Url"]),
            "HTMX should get the card, not a page, and the list's address for the address bar.");
        Assert.Contains(
            "Placeholder Logistics archived.",
            answer.Body,
            "The answer should carry the toast that says what happened.");
    }

    /// <summary>
    /// Integration test to verify that the same archive sent by the browser on its own is redirected back to the list,
    /// so that refreshing the list does not post again.
    /// </summary>
    [TestMethod]
    public async Task PostArchive_WithoutHtmx_RedirectsToTheList()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, "/organizations").ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, new Dictionary<string, string>());

        // Act (When)
        Answer answer = await Requests.PostAsync(browser, ArchiveAddress, HtmxRequest.None, form)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.Redirect, "/organizations"),
            (answer.Status, answer.Headers["Location"]),
            "The browser should be redirected to the list.");
    }

    /// <summary>
    /// Integration test to verify that an archive sent without the antiforgery token is refused with 400 and no body,
    /// an answer no page plans for, which the application's script keeps out of the page.
    /// </summary>
    [TestMethod]
    public async Task PostArchive_WithoutAntiforgeryToken_Answers400WithNoBody()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        Dictionary<string, string> form = new Dictionary<string, string>();

        // Act (When)
        Answer answer = await Requests.PostAsync(browser, ArchiveAddress, HtmxRequest.Partial, form)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.BadRequest, string.Empty),
            (answer.Status, answer.Body),
            "A post without the token should be refused with 400 and nothing to swap in.");
    }

    /// <summary>
    /// Stops the application and deletes its database.
    /// </summary>
    public void Dispose()
    {
        this._application.Dispose();
    }

    #endregion Public Methods
}
