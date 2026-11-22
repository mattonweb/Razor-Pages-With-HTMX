namespace HTMX.Test.Integration.Application.Web.Pages.Shared;

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains tests for what every page of the layout has: the brand's favicon, which the head names and the
/// application serves, and what the application's script does, in a browser: the menu button that switches the side
/// nav between full and an icon rail, and a toast that goes when Dismiss is pressed or once it has faded. The script
/// finds each by an id or a data- attribute, never by the classes that style it, so these tests hold while those
/// classes change.
/// </summary>
[TestClass]
public sealed class LayoutTests : IDisposable
{
    #region Private Fields

    // A page that raises a toast with no Undo: saving a row of the Stock table says so in one.
    private const string StockTable = "/components/stock-table";

    // A link in a page's head that names an icon, as Razor writes it: its rel, then its address.
    private static readonly Regex IconLink = new Regex(
        "<link rel=\"(icon|apple-touch-icon)\" href=\"([^\"]+)\"",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that a page names the brand's three icons in its head, the .ico, then the SVG, then
    /// the touch icon, and that the application answers each address it names with that file.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task Get_AnyPage_NamesTheIconsTheApplicationServes()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        Answer page = await Requests.GetAsync(browser, "/", HtmxRequest.None).ConfigureAwait(false);
        Match[] links = IconLink.Matches(page.Body).ToArray();

        // Act (When)
        (string Rel, HttpStatusCode Status, string Type)[] served =
            await Task.WhenAll(links.Select(link => ServedAsync(browser, link))).ConfigureAwait(false);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[]
            {
                ("icon", HttpStatusCode.OK, "image/x-icon"),
                ("icon", HttpStatusCode.OK, "image/svg+xml"),
                ("apple-touch-icon", HttpStatusCode.OK, "image/png"),
            },
            served,
            "The head should name the .ico, the SVG and the touch icon, and each should be served.");
    }

    /// <summary>
    /// Integration test to verify that /favicon.ico, which a browser asks for by name when a page names no icon, is
    /// served at the root.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task Get_FaviconByName_IsServed()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer icon = await Requests.GetAsync(browser, "/favicon.ico", HtmxRequest.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, "image/x-icon"),
            (icon.Status, icon.Headers["Content-Type"]),
            "/favicon.ico should be served at the root.");
    }

    /// <summary>
    /// Integration test to verify that Edge draws a page's icon from the SVG, which the application answers it with,
    /// rather than taking the .ico. The console reports no error, so the browser found every file it asked for.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task Favicon_AsAPageLoads_IsDrawnFromTheSvg()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            Task<IResponse> icon = page.WaitForResponseAsync(
                static response => response.Url.Contains("/favicon.", StringComparison.Ordinal));

            // Act (When)
            await page.GotoAsync("/").ConfigureAwait(false);
            IResponse answer = await icon.ConfigureAwait(false);
            string type = await answer.HeaderValueAsync("content-type").ConfigureAwait(false) ?? string.Empty;

            // Assert (Then)
            Assert.AreEqual(
                (200, "image/svg+xml"),
                (answer.Status, type),
                "Edge should ask for the SVG icon, and be answered with it: " + answer.Url);
            Assert.IsEmpty(
                edge.ConsoleErrors,
                "The console should report no error: " + string.Join(" | ", edge.ConsoleErrors));
        }
    }

    /// <summary>
    /// Integration test to verify that the menu button switches the side nav, a rail in a window narrower than 900px,
    /// to its full width and back, and says each time whether the nav is expanded. The console reports no error.
    /// </summary>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task MainMenu_PressedTwice_SwitchesTheNavToFullAndBack()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.GotoAsync("/").ConfigureAwait(false);
            ILocator button = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Main menu" });
            ILocator nav = page.GetByRole(AriaRole.Navigation, new PageGetByRoleOptions { Name = "Main" });
            string? railSaid = await button.GetAttributeAsync("aria-expanded").ConfigureAwait(false);
            double railWidth = await WidthAsync(nav).ConfigureAwait(false);

            // Act (When)
            await button.ClickAsync().ConfigureAwait(false);
            string? fullSaid = await button.GetAttributeAsync("aria-expanded").ConfigureAwait(false);
            double fullWidth = await WidthAsync(nav).ConfigureAwait(false);
            await button.ClickAsync().ConfigureAwait(false);
            string? backSaid = await button.GetAttributeAsync("aria-expanded").ConfigureAwait(false);
            double backWidth = await WidthAsync(nav).ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                ("false", "true", "false", true, railWidth),
                (railSaid, fullSaid, backSaid, fullWidth > railWidth, backWidth),
                "The nav should go from a rail to its full width and back, and the button say so each time.");
            Assert.IsEmpty(
                edge.ConsoleErrors,
                "The console should report no error: " + string.Join(" | ", edge.ConsoleErrors));
        }
    }

    /// <summary>
    /// Integration test to verify that a toast's Dismiss takes the toast out of the page at once. The console reports
    /// no error.
    /// </summary>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task Dismiss_OnAToast_RemovesTheToast()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            await RaiseToastAsync(edge).ConfigureAwait(false);
            ILocator dismiss = edge.Toast.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Dismiss" });

            // Act (When)
            await dismiss.ClickAsync().ConfigureAwait(false);
            int toasts = await edge.Toast.CountAsync().ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(0, toasts, "Dismiss should take the toast out of the page.");
            Assert.IsEmpty(
                edge.ConsoleErrors,
                "The console should report no error: " + string.Join(" | ", edge.ConsoleErrors));
        }
    }

    /// <summary>
    /// Integration test to verify that a toast is taken out of the page once it has faded, rather than left there
    /// hidden. Its animations are played to their end at once, as eight seconds and the fade would play them. The
    /// console reports no error.
    /// </summary>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task Toast_OnceItHasFaded_IsRemoved()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            await RaiseToastAsync(edge).ConfigureAwait(false);
            LocatorWaitForOptions gone = new LocatorWaitForOptions { State = WaitForSelectorState.Detached };

            // Act (When)
            await edge.Toast.EvaluateAsync("toast => toast.getAnimations().forEach(a => a.finish())")
                .ConfigureAwait(false);
            await edge.Toast.WaitForAsync(gone).ConfigureAwait(false);
            int toasts = await edge.Toast.CountAsync().ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(0, toasts, "A toast that has faded should be taken out of the page.");
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
    /// Asks for the icon a link names, as a browser would.
    /// </summary>
    /// <param name="browser">The client.</param>
    /// <param name="link">The link, with its rel and its address.</param>
    /// <returns>The link's rel, and the status and type the icon was answered with.</returns>
    private static async Task<(string Rel, HttpStatusCode Status, string Type)> ServedAsync(
        HttpClient browser,
        Match link)
    {
        string address = WebUtility.HtmlDecode(link.Groups[2].Value);
        Answer icon = await Requests.GetAsync(browser, address, HtmxRequest.None).ConfigureAwait(false);
        return (link.Groups[1].Value, icon.Status, icon.Headers["Content-Type"]);
    }

    /// <summary>
    /// Saves a row of the Stock table, and waits for the toast that says so.
    /// </summary>
    /// <param name="edge">The browser.</param>
    /// <returns>A task that completes when the toast is on the page.</returns>
    private static async Task RaiseToastAsync(BrowserSession edge)
    {
        IPage page = edge.Page;
        await page.GotoAsync(StockTable).ConfigureAwait(false);
        ILocator field = page.Locator("#on-hand-10003");
        await field.FillAsync("15").ConfigureAwait(false);
        await field.PressAsync("Enter").ConfigureAwait(false);
        await edge.Toast.WaitForAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Measures how wide an element is drawn.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>Its width in pixels.</returns>
    private static Task<double> WidthAsync(ILocator element)
    {
        return element.EvaluateAsync<double>("e => e.getBoundingClientRect().width");
    }

    #endregion Private Methods
}
