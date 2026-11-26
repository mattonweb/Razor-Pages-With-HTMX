namespace HTMX.Test.Integration.Application.Web.Pages.Components.Icon;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Icon page, at /components/icon, run through the whole application: every icon in
/// the sprite and each size, drawn by the icon's tag helper, which sits in the page's own folder.
/// </summary>
[TestClass]
public sealed class IndexTests : IDisposable
{
    #region Private Fields

    // The page's address.
    private const string Address = "/components/icon";

    // The sprite's address.
    private const string Sprite = "/icons.svg";

    // A symbol of the sprite, by its id.
    private static readonly Regex Symbol = new Regex(
        "<symbol id=\"(?<name>[a-z-]+)\"",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // A page's use of one of the sprite's symbols, as the icon's tag helper and the app shell's logo write it.
    private static readonly Regex SymbolUse = new Regex(
        "<use href=\"/icons\\.svg#(?<name>[a-z-]+)\"",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that the page loaded by the browser on its own is the whole page, with its heading
    /// left unfocused, that it shows every symbol the sprite holds and none it does not, and that the icon's tag
    /// helper, found by the application in its new folder, leaves no icon undrawn.
    /// </summary>
    [TestMethod]
    public async Task Get_LoadedByTheBrowser_ShowsEverySymbolInTheSprite()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        Answer sprite = await Requests.GetAsync(browser, Sprite, HtmxRequest.None).ConfigureAwait(false);

        // Act (When)
        Answer page = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);
        bool undrawn = page.Body.Contains("<icon ", StringComparison.Ordinal);
        string held = NamesIn(Symbol, sprite.Body);
        string shown = NamesIn(SymbolUse, page.Body);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, HttpStatusCode.OK, true, false, false),
            (sprite.Status, page.Status, Html.IsWholePage(page.Body), Html.HeadingFocused(page.Body), undrawn),
            "The page should arrive whole, its heading unfocused, with every icon drawn by the tag helper.");
        Assert.AreEqual(
            held,
            shown,
            "The page should show every symbol the sprite holds, and none it does not.");
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
    /// Reads the symbol names a pattern finds in a text, each once, in order.
    /// </summary>
    /// <param name="pattern">The pattern, whose group <c>name</c> is a symbol's name.</param>
    /// <param name="text">The text.</param>
    /// <returns>The names, each once, in order, joined by spaces.</returns>
    private static string NamesIn(Regex pattern, string text)
    {
        IEnumerable<string> names = pattern.Matches(text).Select(match => match.Groups["name"].Value);
        return string.Join(" ", new SortedSet<string>(names, StringComparer.Ordinal));
    }

    #endregion Private Methods
}
