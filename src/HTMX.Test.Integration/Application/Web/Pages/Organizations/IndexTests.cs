namespace HTMX.Test.Integration.Application.Web.Pages.Organizations;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Playwright;
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
    /// Browser test to verify, in Edge, that when the filter is typed again while the answer to the first filter is
    /// held back, the page gives the first request up at once and the list shows the newer filter's rows, with the
    /// address naming it: the list's one request queue replaces the request on its way (hx-sync), where HTMX's own
    /// queue would wait for the held answer, draw it, and only then send the newer request.
    /// </summary>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task Filter_TypedAgainWhileAnAnswerIsHeld_ShowsTheNewestAnswer()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            TaskCompletionSource held = new TaskCompletionSource();
            ConcurrentQueue<string> givenUp = new ConcurrentQueue<string>();
            page.RequestFailed += (sender, request) => givenUp.Enqueue(request.Url);
            await page.RouteAsync(IsFirstFilter, route => HoldAsync(route, held)).ConfigureAwait(false);
            await page.GotoAsync("/organizations").ConfigureAwait(false);
            ILocator filter = page.Locator("#organization-filter");

            // Act (When)
            Task<IRequest> first = page.WaitForRequestAsync(request => IsFirstFilter(request.Url));
            await filter.FillAsync("Mock").ConfigureAwait(false);
            await first.ConfigureAwait(false);
            Task<IResponse> second = page.WaitForResponseAsync(response => IsSecondFilter(response.Url));
            await filter.FillAsync("Sample").ConfigureAwait(false);
            await second.ConfigureAwait(false);
            await held.Task.ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
            string rows = await page.Locator("#organization-list tbody").InnerTextAsync().ConfigureAwait(false);
            bool newer = rows.Contains("Sample & Sons", StringComparison.Ordinal);
            bool older = rows.Contains("Mockup Media", StringComparison.Ordinal);
            bool firstGivenUp = givenUp.Any(IsFirstFilter);

            // Assert (Then)
            Assert.AreEqual(
                (true, true, false, true),
                (firstGivenUp, newer, older, IsSecondFilter(page.Url)),
                "The first request should be given up, and the list and the address be the newer filter's.");
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
    /// Says whether an address is the list filtered by the first filter the test types.
    /// </summary>
    /// <param name="url">The address.</param>
    /// <returns><see langword="true"/> for the first filter.</returns>
    private static bool IsFirstFilter(string url)
    {
        return url.Contains("filter=Mock", StringComparison.Ordinal);
    }

    /// <summary>
    /// Says whether an address is the list filtered by the second filter the test types.
    /// </summary>
    /// <param name="url">The address.</param>
    /// <returns><see langword="true"/> for the second filter.</returns>
    private static bool IsSecondFilter(string url)
    {
        return url.Contains("filter=Sample", StringComparison.Ordinal);
    }

    /// <summary>
    /// Holds a request's answer back for two seconds, then lets it go on, unless the page has given the request up.
    /// </summary>
    /// <param name="route">The request, as the browser routes it.</param>
    /// <param name="held">Completed once the answer has been let go, or the request was given up.</param>
    /// <returns>A task that completes with the hold.</returns>
    private static async Task HoldAsync(IRoute route, TaskCompletionSource held)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            await route.ContinueAsync().ConfigureAwait(false);
        }
        catch (PlaywrightException)
        {
            // The page gave the request up while it was held, so there is nothing left to let go.
        }
        finally
        {
            held.TrySetResult();
        }
    }

    #endregion Private Methods
}
