namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Seen notices page, at /components/seen-notices, run through the whole
/// application: a notice's request to be marked seen has no form around it, and passes the antiforgery check only
/// with the token the list sends in a header.
/// </summary>
[TestClass]
public sealed class SeenNoticesTests : IDisposable
{
    #region Private Fields

    // The page's address, and the address the fifth notice posts to as it comes into view.
    private const string Address = "/components/seen-notices";
    private const string FifthSeen = "/components/seen-notices?notice=10005&handler=Seen";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that a notice posted with no fields and the headers the list names in its
    /// <c>hx-headers</c>, as HTMX sends it, passes the antiforgery check, answers 204, and is drawn seen next time.
    /// </summary>
    [TestMethod]
    public async Task PostSeen_WithTheListsHeaders_MarksTheNotice()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        Answer page = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);
        Dictionary<string, string> headers = Html.InheritedHxHeaders(page.Body);

        // Act (When)
        Answer seen = await Requests.PostWithoutFormAsync(browser, FifthSeen, HtmxRequest.Partial, headers)
            .ConfigureAwait(false);
        Answer again = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.NoContent, true),
            (seen.Status, headers.ContainsKey("RequestVerificationToken")),
            "The notice should be marked, with the token in the header the antiforgery check reads.");
        Assert.Contains(
            "Notices, 15 new",
            again.Body,
            "The list should be drawn with one notice fewer new.");
    }

    /// <summary>
    /// Integration test to verify that the same request without the list's headers, with nothing to carry the token,
    /// is refused by the antiforgery check with 400.
    /// </summary>
    [TestMethod]
    public async Task PostSeen_WithoutTheHeaders_IsRefused()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);
        Dictionary<string, string> none = new Dictionary<string, string>();

        // Act (When)
        Answer seen = await Requests.PostWithoutFormAsync(browser, FifthSeen, HtmxRequest.Partial, none)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            HttpStatusCode.BadRequest,
            seen.Status,
            "A post with no token should be refused.");
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
