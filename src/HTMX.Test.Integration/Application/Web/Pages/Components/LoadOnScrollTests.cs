namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Load on scroll page, at /components/load-on-scroll, run through the whole
/// application: the last row asks for the next ten as it comes into view, up to a hundred rows; a load it sends moves
/// no keyboard and says what came in the status line, and Load more, pressed, puts the keyboard on the first new row.
/// </summary>
[TestClass]
public sealed class LoadOnScrollTests : IDisposable
{
    #region Private Fields

    // The page's address.
    private const string Address = "/components/load-on-scroll";

    // What the last row says that makes it ask for more as it comes into view.
    private const string Revealed = "hx-trigger=\"revealed\"";

    // The organization a load names, the last of the rows the page shows, in its HTMX address.
    private static readonly Regex LastRow = new Regex(
        "after=(\\d+)",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that only the page's last row asks for more as it comes into view, at the next ten's
    /// address marked as a scrolled load, and that at a hundred rows none does.
    /// </summary>
    /// <param name="shown">How many rows the address asks for.</param>
    /// <param name="growing">How many rows should ask for more.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [DataRow(10, 1, DisplayName = "Ten rows")]
    [DataRow(100, 0, DisplayName = "A hundred rows")]
    public async Task Get_ThePage_OnlyItsLastRowGrowsIt(int shown, int growing)
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer page = await Requests.GetAsync(browser, $"{Address}?shown={shown}", HtmxRequest.None)
            .ConfigureAwait(false);
        int asking = page.Body.Split(Revealed).Length - 1;
        int lastRow = page.Body.IndexOf($"<tr id=\"more-{shown}\" hx-get=\"", StringComparison.Ordinal);

        // Assert (Then)
        Assert.AreEqual(
            (growing, growing == 1),
            (asking, lastRow >= 0),
            "Only the last row should ask for more, and none at a hundred.");
    }

    /// <summary>
    /// Integration test to verify that a load the last row sends answers the next ten with no row taking the keyboard,
    /// and writes what came into the status line, where a load Load more sends puts the keyboard on the first new row
    /// and writes nothing there.
    /// </summary>
    /// <param name="scrolled">Whether the last row sent the load, rather than a press of Load more.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [DataRow(true, DisplayName = "The last row, as it comes into view")]
    [DataRow(false, DisplayName = "Load more, pressed")]
    public async Task Get_ALoadByHtmx_TakesTheKeyboardOnlyWhenPressed(bool scrolled)
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        Answer page = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);
        string last = LastRow.Match(page.Body).Groups[1].Value;
        string marker = scrolled ? "&scrolled=true" : string.Empty;

        // Act (When)
        Answer more = await Requests.GetAsync(browser, $"{Address}?shown=20&after={last}{marker}", HtmxRequest.Partial)
            .ConfigureAwait(false);
        bool focused = more.Body.Contains("autofocus", StringComparison.Ordinal);
        bool said = more.Body.Contains("hx-target=\"#more-status\"", StringComparison.Ordinal);

        // Assert (Then)
        Assert.AreEqual(
            (!scrolled, scrolled, "/components/load-on-scroll?shown=20"),
            (focused, said, more.Headers["HX-Replace-Url"]),
            "Only a pressed load should take the keyboard, and only a scrolled one write to the status line.");
    }

    /// <summary>
    /// Browser test to verify, in Edge, that scrolling the last row into view adds the next ten after it, moves no
    /// keyboard, says what came in the status line, and puts the new count in the address. The console reports no
    /// error.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task Scroll_ToTheLastRow_AddsTheNextTenWithoutMovingTheKeyboard()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.GotoAsync(Address).ConfigureAwait(false);
            int before = await page.Locator("#more-rows tr").CountAsync().ConfigureAwait(false);

            // Act (When)
            await page.Locator("#more-10").ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
            await page.Locator("#more-11").WaitForAsync().ConfigureAwait(false);
            await page.WaitForURLAsync("**?shown=20").ConfigureAwait(false);
            string focused = await page.EvaluateAsync<string>("() => document.activeElement.tagName")
                .ConfigureAwait(false);
            string? said = await page.Locator("#more-status").TextContentAsync().ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                (10, "BODY", "Organizations 11 to 20 added; 20 of 946 shown."),
                (before, focused, said?.Trim()),
                "Ten rows should come after the tenth, the keyboard stay put, and the status line say so.");
            Assert.IsEmpty(
                edge.ConsoleErrors,
                "The console should report no error: " + string.Join(" | ", edge.ConsoleErrors));
        }
    }

    /// <summary>
    /// Browser test to verify, in Edge, that the keyboard on the last row's link, which brings the row into view, goes
    /// on into the rows that load brings with Tab, since they come after it.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task Tab_FromTheLastRow_GoesOnIntoTheNewRows()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.GotoAsync(Address).ConfigureAwait(false);

            // Act (When)
            await page.Locator("#more-10 a").FocusAsync().ConfigureAwait(false);
            await page.Locator("#more-11").WaitForAsync().ConfigureAwait(false);
            await page.Keyboard.PressAsync("Tab").ConfigureAwait(false);
            string? row = await page.EvaluateAsync<string?>("() => document.activeElement.closest('tr')?.id")
                .ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                "more-11",
                row,
                "Tab from the tenth row should go on to the eleventh.");
        }
    }

    /// <summary>
    /// Browser test to verify, in Edge, that the list stops growing at a hundred rows, however far it is scrolled: the
    /// hundredth row asks for no more, and the pager then says where the rest are. Each earlier last row keeps its
    /// request, which <c>revealed</c> sends once.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task Scroll_ToTheEnd_StopsAtAHundredRows()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.GotoAsync(Address).ConfigureAwait(false);

            // Act (When)
            for (int shown = 10; shown < 100; shown += 10)
            {
                await page.Locator($"#more-{shown}").ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
                await page.Locator($"#more-{shown + 1}").WaitForAsync().ConfigureAwait(false);
            }

            await page.Locator("#more-100").ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
            await page.WaitForURLAsync("**?shown=100").ConfigureAwait(false);
            int rows = await page.Locator("#more-rows tr").CountAsync().ConfigureAwait(false);
            int asking = await page.Locator("#more-100[hx-get]").CountAsync().ConfigureAwait(false);
            string? pager = await page.Locator("#more-pager").TextContentAsync().ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                (100, 0),
                (rows, asking),
                "The list should stop at a hundred rows, the last asking for no more.");
            Assert.Contains(
                "The list stops at 100",
                pager ?? string.Empty,
                "The pager should say where the rest are.");
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
}
