namespace HTMX.Test.Integration.Application.Web.Pages.Organizations;

using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for an organization's pages, at /organizations/{id} and its tabs, run through the whole
/// application: what an identifier that names no organization answers to each kind of request, and how the page and
/// its history are laid out for assistive technology.
/// </summary>
[TestClass]
public sealed class DetailsTests : IDisposable
{
    #region Private Fields

    // A diff's frame that the keyboard reaches, named by the caption of the change it holds.
    private static readonly Regex FocusableDiffFrame = new Regex(
        "tabindex=\"0\" role=\"region\"\\s+aria-label=\"Changes in version \\d+\"",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // A diff's table, by its caption.
    private static readonly Regex DiffTable = new Regex(
        "<caption[^>]*>Changes in version \\d+</caption>",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that an identifier that names no organization gets the whole page saying so, with
    /// 404, and that the page's heading takes the keyboard when a boosted link loaded it.
    /// </summary>
    /// <param name="kind">What sends the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="headingFocused">Whether the page's heading should take the keyboard.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The browser on its own")]
    [DataRow(nameof(HtmxRequest.Boosted), true, DisplayName = "A boosted link")]
    public async Task Get_UnknownOrganization_Answers404WithThePageSayingSo(string kind, bool headingFocused)
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer answer = await Requests.GetAsync(browser, "/organizations/99999", Enum.Parse<HtmxRequest>(kind))
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.NotFound, true, headingFocused),
            (answer.Status, Html.IsWholePage(answer.Body), Html.HeadingFocused(answer.Body)),
            "An unknown organization should get the whole page with 404, its heading focused only when HTMX loaded it.");
        Assert.Contains(
            "Organization not found",
            answer.Body,
            "The page should say that no organization has the identifier.");
    }

    /// <summary>
    /// Integration test to verify that the page for an identifier that names no organization says so in an h2, one
    /// level under the page's h1, as a screen reader's list of headings expects.
    /// </summary>
    [TestMethod]
    public async Task Get_UnknownOrganization_SaysSoOneLevelUnderThePageHeading()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer answer = await Requests.GetAsync(browser, "/organizations/99999", HtmxRequest.None)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.Contains(
            "<h2>No organization has the identifier 99999</h2>",
            answer.Body,
            "The empty state's heading should be an h2 under the page's h1.");
    }

    /// <summary>
    /// Integration test to verify that each change on an organization's history is in a frame the keyboard reaches,
    /// named by the change's caption, since the frame scrolls sideways on a narrow screen and nothing in it takes the
    /// keyboard.
    /// </summary>
    [TestMethod]
    public async Task Get_History_PutsEachChangeInAFrameTheKeyboardReaches()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer answer = await Requests.GetAsync(browser, "/organizations/10003/history", HtmxRequest.None)
            .ConfigureAwait(false);
        int frames = FocusableDiffFrame.Matches(answer.Body).Count;
        int diffs = DiffTable.Matches(answer.Body).Count;

        // Assert (Then)
        Assert.AreEqual(
            (true, diffs),
            (diffs > 0, frames),
            "Every change on the history should be in a frame that takes the keyboard and has a name.");
    }

    /// <summary>
    /// Integration test to verify that a tab HTMX asks for part of, whose organization has gone, answers 204 with
    /// HX-Location naming the tab, so HTMX loads the whole page that says so instead of nesting it in the tab.
    /// </summary>
    [TestMethod]
    public async Task Get_UnknownOrganizationForPartial_Answers204AndLoadsTheWholePage()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer answer = await Requests.GetAsync(browser, "/organizations/99999/contacts", HtmxRequest.Partial)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.NoContent, "/organizations/99999/contacts"),
            (answer.Status, answer.Headers["HX-Location"]),
            "HTMX should get nothing to swap and the tab's whole page to load.");
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
