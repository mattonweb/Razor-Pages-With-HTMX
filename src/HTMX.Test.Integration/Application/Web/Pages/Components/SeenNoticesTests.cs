namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HTMX.Application.Web.Pages.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Seen notices page, at /components/seen-notices, run through the whole
/// application: a notice's request to be marked seen has no form around it, and passes the antiforgery check only
/// with the token the list sends in a header.
/// </summary>
[TestClass]
public sealed class SeenNoticesTests : IDisposable
{
    #region Private Fields

    // The page's address, and the address the fifth notice posts to as it comes into view.
    private const string Address = "/components/seen-notices";
    private const string FifthSeen = "/components/seen-notices?notice=10005&handler=Seen";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that a notice posted with no fields and the headers the list names in its
    /// <c>hx-headers</c>, as HTMX sends it, passes the antiforgery check, answers 204, and is drawn seen next time.
    /// </summary>
    [TestMethod]
    public async Task PostSeen_WithTheListsHeaders_MarksTheNotice()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        Answer page = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);
        Dictionary<string, string> headers = Html.InheritedHxHeaders(page.Body);

        // Act (When)
        Answer seen = await Requests.PostWithoutFormAsync(browser, FifthSeen, HtmxRequest.Partial, headers)
            .ConfigureAwait(false);
        Answer again = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.NoContent, true),
            (seen.Status, headers.ContainsKey("RequestVerificationToken")),
            "The notice should be marked, with the token in the header the antiforgery check reads.");
        Assert.Contains(
            "Notices, 15 new",
            again.Body,
            "The list should be drawn with one notice fewer new.");
    }

    /// <summary>
    /// Integration test to verify that the same request without the list's headers, with nothing to carry the token,
    /// is refused by the antiforgery check with 400.
    /// </summary>
    [TestMethod]
    public async Task PostSeen_WithoutTheHeaders_IsRefused()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);
        Dictionary<string, string> none = new Dictionary<string, string>();

        // Act (When)
        Answer seen = await Requests.PostWithoutFormAsync(browser, FifthSeen, HtmxRequest.Partial, none)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            HttpStatusCode.BadRequest,
            seen.Status,
            "A post with no token should be refused.");
    }

    /// <summary>
    /// Browser test to verify, in Edge at 800 by 600, that the notices in view mark themselves seen as the page loads
    /// and the others once they are scrolled into view, each request carrying the list's antiforgery header: the
    /// board, read on the server, ends with all sixteen seen. The console reports no error.
    /// </summary>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task Notices_ScrolledIntoView_MarkThemselvesSeen()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        NoticeBoard board = this._application.Services.GetRequiredService<NoticeBoard>();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;

            // Act (When)
            await page.GotoAsync(Address).ConfigureAwait(false);
            int seenOnLoad = await SeenSettledAsync(board).ConfigureAwait(false);
            await page.Locator("#notice-10016").ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
            int seenAfterScroll = await SeenSettledAsync(board).ConfigureAwait(false);

            // Assert (Then)
            Assert.IsTrue(
                seenOnLoad > 0 && seenOnLoad < 16,
                $"Only the notices in view should be seen as the page loads, not {seenOnLoad}.");
            Assert.AreEqual(
                16,
                seenAfterScroll,
                "Every notice should be seen once scrolled into view.");
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
    /// Waits for the requests the page sends as notices come into view to have landed, then counts the notices seen:
    /// the count is read again every tenth of a second until it has not changed for half a second.
    /// </summary>
    /// <param name="board">The board, as the application holds it.</param>
    /// <returns>How many notices are seen.</returns>
    private static async Task<int> SeenSettledAsync(NoticeBoard board)
    {
        int seen = -1;
        int steady = 0;
        while (steady < 5)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
            int now = Array.FindAll(board.All(), static notice => notice.Seen).Length;
            steady = now == seen ? steady + 1 : 0;
            seen = now;
        }

        return seen;
    }

    #endregion Private Methods
}
