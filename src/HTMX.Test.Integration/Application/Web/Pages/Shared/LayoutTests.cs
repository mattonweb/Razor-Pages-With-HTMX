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

    // How much wider than the window the page is: more than nothing, and the window scrolls sideways.
    private const string WiderThanTheWindow =
        "() => document.documentElement.scrollWidth - document.documentElement.clientWidth";

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
    /// Browser test to verify, in Edge, that in a window too narrow for the full nav and the page side by side, 320px
    /// wide, the menu button opens the full nav across the window's width, with the page hidden under it and nothing
    /// to scroll sideways; and that Escape, pressed with the keyboard in the nav, closes it as the button does, gives
    /// the keyboard to the button and shows the page again. The console reports no error.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task MainMenu_InANarrowWindow_FillsTheWindowUntilEscape()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.SetViewportSizeAsync(320, 600).ConfigureAwait(false);
            await page.GotoAsync("/organizations").ConfigureAwait(false);
            ILocator button = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Main menu" });
            ILocator nav = page.GetByRole(AriaRole.Navigation, new PageGetByRoleOptions { Name = "Main" });
            ILocator main = page.Locator("#main");
            ILocator users = nav.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Users" });

            // Act (When)
            await button.ClickAsync().ConfigureAwait(false);
            double openWidth = await WidthAsync(nav).ConfigureAwait(false);
            bool shownOpen = await main.IsVisibleAsync().ConfigureAwait(false);
            int wider = await page.EvaluateAsync<int>(WiderThanTheWindow).ConfigureAwait(false);
            await users.FocusAsync().ConfigureAwait(false);
            await page.Keyboard.PressAsync("Escape").ConfigureAwait(false);
            double closedWidth = await WidthAsync(nav).ConfigureAwait(false);
            bool shownClosed = await main.IsVisibleAsync().ConfigureAwait(false);
            bool buttonHasKeyboard = await button.EvaluateAsync<bool>("b => b === document.activeElement")
                .ConfigureAwait(false);
            string? said = await button.GetAttributeAsync("aria-expanded").ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                (320d, false, 0, 48d, true, true, "false"),
                (openWidth, shownOpen, wider, closedWidth, shownClosed, buttonHasKeyboard, said),
                "The nav should fill the window over the hidden page, and Escape give the page and the keyboard back.");
            Assert.IsEmpty(
                edge.ConsoleErrors,
                "The console should report no error: " + string.Join(" | ", edge.ConsoleErrors));
        }
    }

    /// <summary>
    /// Browser test to verify, in Edge, that the keyboard's focus shows as an outline on each kind of control, in the
    /// page's own colors and in forced colors, as a Windows contrast theme sets them, which take every shadow away: the
    /// side nav's current link and the current segment, which have shadows of their own, a button, and the filter box,
    /// a form control, whose outline is 1px against its border in the page's colors and the full 2px in forced
    /// colors. The console reports no error.
    /// </summary>
    /// <param name="forced">Whether the page is drawn in forced colors.</param>
    /// <param name="control">The width of a form control's outline.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    [DataRow(false, "1px", DisplayName = "In the page's own colors")]
    [DataRow(true, "2px", DisplayName = "In forced colors")]
    public async Task Tab_OntoEachKindOfControl_DrawsAnOutline(bool forced, string control)
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            ForcedColors colors = forced ? ForcedColors.Active : ForcedColors.None;
            await page.EmulateMediaAsync(new PageEmulateMediaOptions { ForcedColors = colors }).ConfigureAwait(false);
            await page.GotoAsync("/organizations").ConfigureAwait(false);

            // Act (When)
            string navLink = await OutlineWithFocusAsync(page, "#app-nav a[aria-current=page]").ConfigureAwait(false);
            string segment = await OutlineWithFocusAsync(page, "main a[aria-current=page]").ConfigureAwait(false);
            string button = await OutlineWithFocusAsync(page, "main button[popovertarget]").ConfigureAwait(false);
            string filter = await OutlineWithFocusAsync(page, "main input[type=search]").ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                ("solid 2px", "solid 2px", "solid 2px", "solid " + control),
                (navLink, segment, button, filter),
                "Each control should draw an outline with the keyboard on it.");
            Assert.IsEmpty(
                edge.ConsoleErrors,
                "The console should report no error: " + string.Join(" | ", edge.ConsoleErrors));
        }
    }

    /// <summary>
    /// Browser test to verify, in Edge, that a link among the words of a paragraph is underlined, since its color alone
    /// does not set it apart from the text around it, and that a link standing on its own, a grid's title, is not, nor
    /// a related record's link in a sentence, which its icon sets apart. The console reports no error.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task Load_ALinkInAParagraph_IsUnderlined()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;

            // Act (When)
            await page.GotoAsync("/components/not-allowed").ConfigureAwait(false);
            string inText = await DecorationAsync(page.Locator("main p a").First).ConfigureAwait(false);
            await page.GotoAsync("/organizations").ConfigureAwait(false);
            string alone = await DecorationAsync(page.Locator("main td a").First).ConfigureAwait(false);
            await page.GotoAsync("/organizations/10003").ConfigureAwait(false);
            string related = await DecorationAsync(page.Locator("main header p a").First).ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                ("underline", "none", "none"),
                (inText, alone, related),
                "A link in a paragraph should be underlined, and neither a grid's title nor a related record's link.");
            Assert.IsEmpty(
                edge.ConsoleErrors,
                "The console should report no error: " + string.Join(" | ", edge.ConsoleErrors));
        }
    }

    /// <summary>
    /// Browser test to verify, in Edge, that a page fits a window 320px wide, a narrow phone's and a 1280px window's
    /// at 400% zoom, without scrolling sideways: a segmented control too wide for its card wraps, a filter box gives
    /// its input's room to the Apply button it shows without HTMX, and a grid or a diff too wide for its card scrolls
    /// inside its own frame, with the labels hidden for assistive technology inside it too. The console reports no
    /// error.
    /// </summary>
    /// <param name="path">The page.</param>
    /// <param name="scripting">How much script the page runs, by name.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    [DataRow("/organizations", nameof(Scripting.On), DisplayName = "Organizations, whose segmented control wraps")]
    [DataRow(
        "/organizations",
        nameof(Scripting.WithoutHtmx),
        DisplayName = "Organizations without HTMX, whose filter box shows Apply")]
    [DataRow("/users", nameof(Scripting.On), DisplayName = "Users, whose segmented control wraps")]
    [DataRow(
        "/organizations/10003/history",
        nameof(Scripting.On),
        DisplayName = "An organization's history, whose diff scrolls")]
    [DataRow(StockTable, nameof(Scripting.On), DisplayName = "Stock table, whose rows' buttons have hidden labels")]
    [DataRow(
        "/components/not-allowed",
        nameof(Scripting.On),
        DisplayName = "Not allowed, whose rows' buttons have hidden labels")]
    public async Task Load_InAWindow320PixelsWide_DoesNotScrollSideways(string path, string scripting)
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Enum.Parse<Scripting>(scripting))
            .ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.SetViewportSizeAsync(320, 600).ConfigureAwait(false);

            // Act (When)
            await page.GotoAsync(path).ConfigureAwait(false);
            int wider = await page.EvaluateAsync<int>(WiderThanTheWindow).ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(0, wider, "The page should be no wider than the window.");
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
    /// Puts the keyboard on the first control a selector finds, as Tab does: focuses it, then steps back and forward
    /// again, so that it shows its focus indicator; then reads the outline it draws.
    /// </summary>
    /// <param name="page">The page.</param>
    /// <param name="selector">Finds the control.</param>
    /// <returns>The outline's style and width, or "not focused" when the keyboard did not land on the control.</returns>
    private static async Task<string> OutlineWithFocusAsync(IPage page, string selector)
    {
        await page.Locator(selector).First.FocusAsync().ConfigureAwait(false);
        await page.Keyboard.PressAsync("Shift+Tab").ConfigureAwait(false);
        await page.Keyboard.PressAsync("Tab").ConfigureAwait(false);
        return await page.EvaluateAsync<string>(
            "selector => { const e = document.activeElement; const s = getComputedStyle(e); "
            + "return e.matches(selector) && e.matches(':focus-visible') "
            + "? `${s.outlineStyle} ${s.outlineWidth}` : 'not focused'; }",
            selector).ConfigureAwait(false);
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
    /// Reads the lines an element's text is drawn with.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>Its text-decoration-line, such as underline, or none.</returns>
    private static Task<string> DecorationAsync(ILocator element)
    {
        return element.EvaluateAsync<string>("e => getComputedStyle(e).textDecorationLine");
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
