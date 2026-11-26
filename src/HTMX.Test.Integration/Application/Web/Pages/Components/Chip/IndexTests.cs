namespace HTMX.Test.Integration.Application.Web.Pages.Components.Chip;

using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Chip page, at /components/chip, run through the whole application: every tone of
/// the chip, drawn by the chip's tag helper, which sits in the page's own folder.
/// </summary>
[TestClass]
public sealed class IndexTests : IDisposable
{
    #region Private Fields

    // The page's address.
    private const string Address = "/components/chip";

    // An element that still carries the chip attribute, which the chip's tag helper takes away when it draws one.
    private static readonly Regex UndrawnChip = new Regex(
        "<[a-z]+\\b[^>]*\\schip=\"",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // A chip the tag helper drew, by the utilities every tone shares: a full rounding and a dot before its label.
    private static readonly Regex DrawnChip = new Regex(
        "<span class=\"inline-flex h-6 [^\"]*rounded-full[^\"]*before:size-2",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that the page loaded by the browser on its own is the whole page, with its heading
    /// left unfocused, and that the chip's tag helper, found by the application in its new folder, draws all six tones
    /// on it, leaving no element with the attribute that names a tone.
    /// </summary>
    [TestMethod]
    public async Task Get_LoadedByTheBrowser_DrawsEveryChipWithTheTagHelper()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer page = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);
        int undrawn = UndrawnChip.Count(page.Body);
        int drawn = DrawnChip.Count(page.Body);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, true, false, 0, 6),
            (page.Status, Html.IsWholePage(page.Body), Html.HeadingFocused(page.Body), undrawn, drawn),
            "The page should arrive whole, its heading unfocused, with all six tones drawn by the tag helper.");
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
