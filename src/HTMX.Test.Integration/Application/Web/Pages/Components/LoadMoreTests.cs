namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Load more page, at /components/load-more, run through the whole application:
/// the first ten rows, and HTMX's Load more of the next ten after the last, by key.
/// </summary>
[TestClass]
public sealed class LoadMoreTests : IDisposable
{
    #region Private Fields

    // The page's address.
    private const string Address = "/components/load-more";

    // The organization Load more names, the last of the rows the page shows, in its HTMX address.
    private static readonly Regex LastRow = new Regex(
        "after=(\\d+)",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // A row, by the id that numbers it.
    private static readonly Regex Row = new Regex(
        "<tr id=\"more-\\d+\"",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that the page shows ten rows, and that HTMX's Load more, naming the last of them,
    /// answers the next ten, from row 11, with the footer in an hx-partial and the page's new address.
    /// </summary>
    [TestMethod]
    public async Task Get_LoadMoreByHtmx_AnswersTheNextTen()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        Answer page = await Requests.GetAsync(browser, Address, HtmxRequest.None).ConfigureAwait(false);
        string last = LastRow.Match(page.Body).Groups[1].Value;

        // Act (When)
        Answer more = await Requests.GetAsync(browser, $"{Address}?shown=20&after={last}", HtmxRequest.Partial)
            .ConfigureAwait(false);

        // Assert (Then)
        int rowsShown = Row.Count(page.Body);
        int rowsAdded = Row.Count(more.Body);
        Assert.AreEqual(
            (10, HttpStatusCode.OK, 10, false, "/components/load-more?shown=20"),
            (rowsShown, more.Status, rowsAdded, Html.IsWholePage(more.Body), more.Headers["HX-Replace-Url"]),
            "The page should show ten rows, and Load more answer ten more with the new address.");
        Assert.Contains(
            "<tr id=\"more-11\"",
            more.Body,
            "Load more's rows should start at row 11.");
        Assert.Contains(
            "<hx-partial hx-target=\"#more-footer\"",
            more.Body,
            "Load more should draw the footer again in the same answer.");
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
