namespace HTMX.Test.Integration.Application.Web.Pages.Shared;

using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains browser tests for what the application's script does on every page of the layout: the menu button that
/// switches the side nav between full and an icon rail, and a toast that goes when Dismiss is pressed or once it has
/// faded. The script finds each by an id or a data- attribute, never by the classes that style it, so these tests hold
/// while those classes change.
/// </summary>
[TestClass]
public sealed class LayoutTests : IDisposable
{
    #region Private Fields

    // A page that raises a toast with no Undo: saving a row of the Stock table says so in one.
    private const string StockTable = "/components/stock-table";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

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
