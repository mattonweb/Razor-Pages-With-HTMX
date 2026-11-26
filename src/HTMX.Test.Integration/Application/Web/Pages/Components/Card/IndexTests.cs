namespace HTMX.Test.Integration.Application.Web.Pages.Components.Card;

using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Card page, at /components/card, run through the whole application: the card and
/// each of its parts, drawn by the card's tag helper, which sits in the page's own folder.
/// </summary>
[TestClass]
public sealed class IndexTests : IDisposable
{
    #region Private Fields

    // The page's address.
    private const string Address = "/components/card";

    // An element that still carries the card attribute, which the card's tag helper takes away when it draws one.
    private static readonly Regex UndrawnCard = new Regex(
        "<[a-z]+\\b[^>]*\\scard[=\\s>]",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // A card the tag helper drew, by the shadow only the card itself has.
    private static readonly Regex DrawnCard = new Regex(
        "class=\"[^\"]*\\bshadow-card\\b",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that the page loaded by the browser on its own is the whole page, with its heading
    /// left unfocused, and that the card's tag helper, found by the application in its new folder, draws the page's
    /// four cards, leaving no element with the attribute that names a card or one of its parts.
    /// </summary>
    [TestMethod]
    public async Task Get_LoadedByTheBrowser_DrawsEveryCardWithTheTagHelper()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer page = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);
        int undrawn = UndrawnCard.Count(page.Body);
        int drawn = DrawnCard.Count(page.Body);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, true, false, 0, 4),
            (page.Status, Html.IsWholePage(page.Body), Html.HeadingFocused(page.Body), undrawn, drawn),
            "The page should arrive whole, its heading unfocused, with its four cards drawn by the tag helper.");
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
