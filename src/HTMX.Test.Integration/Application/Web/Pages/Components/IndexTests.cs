namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Components page, at /components, run in Edge against the whole application:
/// every link is boosted, so HTMX loads the next page in place and the keyboard goes to its heading, Back included.
/// </summary>
[TestClass]
public sealed class IndexTests : IDisposable
{
    #region Private Fields

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Browser test to verify, in Edge, that a boosted link loads the next page in place, with its address pushed and
    /// the keyboard on its heading, and that Back, which HTMX answers by asking for the page again, puts the keyboard
    /// on the earlier page's heading. The console reports no error.
    /// </summary>
    [TestMethod]
    [TestCategory("Browser")]
    [DoNotParallelize]
    public async Task BoostedLink_ThenBack_PutsTheKeyboardOnEachHeading()
    {
        // Arrange (Given)
        Uri application = this._application.StartServerForBrowser();
        BrowserSession edge = await BrowserSession.OpenAsync(application, Scripting.On).ConfigureAwait(false);
        await using (edge.ConfigureAwait(false))
        {
            IPage page = edge.Page;
            await page.GotoAsync("/components").ConfigureAwait(false);
            ILocator focusedHeading = page.Locator("h1:focus");

            // Act (When)
            await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Load more", Exact = true })
                .ClickAsync()
                .ConfigureAwait(false);
            await page.WaitForURLAsync("**/components/load-more").ConfigureAwait(false);
            string? forward = await focusedHeading.TextContentAsync().ConfigureAwait(false);
            await page.GoBackAsync().ConfigureAwait(false);
            await page.WaitForURLAsync("**/components").ConfigureAwait(false);
            await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Components", Level = 1 })
                .WaitForAsync()
                .ConfigureAwait(false);
            string? back = await focusedHeading.TextContentAsync().ConfigureAwait(false);

            // Assert (Then)
            Assert.AreEqual(
                ("Load more", "Components"),
                (forward, back),
                "Each page's heading should take the keyboard, Back's included.");
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
}
