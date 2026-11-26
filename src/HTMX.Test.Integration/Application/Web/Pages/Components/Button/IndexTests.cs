namespace HTMX.Test.Integration.Application.Web.Pages.Components.Button;

using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Button page, at /components/button, run through the whole application: every
/// look and state of the button, drawn by the button's tag helper, which sits in the page's own folder.
/// </summary>
[TestClass]
public sealed class IndexTests : IDisposable
{
    #region Private Fields

    // The page's address.
    private const string Address = "/components/button";

    // An element that still carries the button attribute, which the button's tag helper takes away when it draws one.
    private static readonly Regex UndrawnButton = new Regex(
        "<[a-z]+\\b[^>]*\\sbutton=\"",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that the page loaded by the browser on its own is the whole page, with its heading
    /// left unfocused, and that the button's tag helper, found by the application in its new folder, draws every
    /// sample on it and every button in its layout, leaving no element with the attribute that names a look.
    /// </summary>
    [TestMethod]
    public async Task Get_LoadedByTheBrowser_DrawsEveryButtonWithTheTagHelper()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer page = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, true, false, 0),
            (page.Status, Html.IsWholePage(page.Body), Html.HeadingFocused(page.Body), UndrawnButton.Count(page.Body)),
            "The page should arrive whole, its heading unfocused, with every button drawn by the tag helper.");
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
