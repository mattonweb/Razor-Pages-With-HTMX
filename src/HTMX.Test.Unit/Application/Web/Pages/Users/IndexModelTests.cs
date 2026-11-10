namespace HTMX.Test.Unit.Application.Web.Pages.Users;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages;
using HTMX.Application.Web.Pages.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the users' <see cref="IndexModel"/> class: which response the
/// list page sends to which request, how it reads its query string, the links its headers and status filter write, and
/// the toast a deleted user's page leaves for it.
/// </summary>
[TestClass]
public sealed class IndexModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that HTMX swapping part of the page gets only the list's card, and anything else, the
    /// browser itself or HTMX restoring a page for the back button, gets the whole page; every answer says it varies
    /// by the header that chose it.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="partial">Whether the answer should be the list's card alone.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The browser itself")]
    [DataRow(nameof(HtmxRequest.Partial), true, DisplayName = "HTMX swapping the card")]
    [DataRow(nameof(HtmxRequest.HistoryRestore), false, DisplayName = "HTMX restoring the page")]
    public async Task OnGetAsync_EachKindOfRequest_AnswersWithTheCardOrThePage(string kind, bool partial)
    {
        // Arrange (Given)
        IndexModel page = CreatePage(new StubUserService(), Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            partial ? "_UserList" : null,
            (result as PartialViewResult)?.ViewName,
            "Only HTMX swapping part of the page should get the list's card alone.");
        Assert.AreEqual(
            partial,
            result is not PageResult,
            "Every other request should get the whole page.");
        Assert.AreEqual(
            "HX-Request-Type",
            page.Response.Headers.Vary.ToString(),
            "The answer should vary by the header that chose it.");
    }

    /// <summary>
    /// Unit test to verify that values the page does not recognize ask for the defaults: active users by name, A to Z,
    /// ten to a page.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_UnknownValues_AsksForTheDefaults()
    {
        // Arrange (Given)
        StubUserService service = new StubUserService();
        IndexModel page = CreatePage(service, HtmxRequest.None);
        page.Input = new ListInput
        {
            Status = "retired",
            Sort = "age",
            Direction = "sideways",
            PageSize = 7,
        };

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        UserCollectionRequest? sent = service.LastRequest();

        // Assert (Then)
        Assert.AreEqual(
            (RecordStatus.Active, UserSortColumn.Name, SortDirection.Ascending, new PageSize(10)),
            (sent?.Status, sent?.Sort, sent?.Direction, sent?.PageSize),
            "Unknown values should fall back to active users by name, A to Z, ten to a page.");
    }

    /// <summary>
    /// Unit test to verify that the query string's values, in any capitals, ask for that view of the list.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_KnownValues_AsksForThatView()
    {
        // Arrange (Given)
        StubUserService service = new StubUserService();
        IndexModel page = CreatePage(service, HtmxRequest.Partial);
        page.Input = new ListInput
        {
            Filter = "example.net",
            Status = "ARCHIVED",
            Sort = "Email",
            Direction = "descending",
            Before = 10007,
            PageSize = 25,
        };

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        UserCollectionRequest? sent = service.LastRequest();

        // Assert (Then)
        Assert.AreEqual(
            ("example.net", RecordStatus.Archived, UserSortColumn.Email, SortDirection.Descending),
            (sent?.Filter?.Value, sent?.Status, sent?.Sort, sent?.Direction),
            "The filter, status and sort should be the ones the query string names.");
        Assert.AreEqual(
            ((UserCursor?)new UserCursor(new UserId(10007), CursorPosition.Before), new PageSize(25)),
            (sent?.Cursor, sent?.PageSize),
            "The page should end before user 10007, 25 to a page.");
    }

    /// <summary>
    /// Unit test to verify what a sort header's link asks for on a list sorted by name, A to Z: the Name header turns
    /// the order round, a text column sorts A to Z, and Updated sorts the most recent first. Each starts again at the
    /// first page.
    /// </summary>
    /// <param name="column">The header's column.</param>
    /// <param name="sort">The sort the link should write.</param>
    /// <param name="direction">The direction the link should write.</param>
    [TestMethod]
    [DataRow(UserSortColumn.Name, "name", "descending", DisplayName = "The sorted column turns round")]
    [DataRow(UserSortColumn.Username, "username", "ascending", DisplayName = "Username A to Z")]
    [DataRow(UserSortColumn.Updated, "updated", "descending", DisplayName = "Updated most recent first")]
    public async Task SortRouteValues_ListByName_WritesTheHeadersSort(
        UserSortColumn column,
        string sort,
        string direction)
    {
        // Arrange (Given)
        IndexModel page = CreatePage(new StubUserService(), HtmxRequest.None);
        page.Input = new ListInput { After = 10020 };
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Act (When)
        Dictionary<string, string> values = page.SortRouteValues(column);

        // Assert (Then)
        Assert.AreEqual(
            (sort, direction, false),
            (values["sort"], values["dir"], values.ContainsKey("after")),
            "The link should sort by the header's column, the way expected, from the first page.");
    }

    /// <summary>
    /// Unit test to verify that the status filter's links keep the filter and sort, start again at the first page,
    /// and leave out the default status.
    /// </summary>
    [TestMethod]
    public async Task RouteValues_StatusLinks_StartAgainAtTheFirstPage()
    {
        // Arrange (Given)
        IndexModel page = CreatePage(new StubUserService(), HtmxRequest.None);
        page.Input = new ListInput { Filter = "sam", Sort = "updated", After = 10020 };
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Act (When)
        Dictionary<string, string> archived = page.RouteValues(status: RecordStatus.Archived);
        Dictionary<string, string> active = page.RouteValues(status: RecordStatus.Active);

        // Assert (Then)
        CollectionAssert.AreEquivalent(
            new[] { "filter", "status", "sort" },
            archived.Keys,
            "Archived should keep the filter and the sort, and drop the cursor.");
        CollectionAssert.AreEquivalent(
            new[] { "filter", "sort" },
            active.Keys,
            "Active is the default status, so its link should leave it out.");
    }

    /// <summary>
    /// Unit test to verify that the whole page shows the toast a deleted user's page left in TempData, and that a swap
    /// of the list's card leaves it waiting.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="shown">Whether the toast should be shown.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.None), true, DisplayName = "The whole page, after a redirect")]
    [DataRow(nameof(HtmxRequest.HistoryRestore), true, DisplayName = "The whole page, loaded by HTMX")]
    [DataRow(nameof(HtmxRequest.Partial), false, DisplayName = "A swap of the card")]
    public async Task OnGetAsync_ToastLeftByADelete_ShowsItOnTheWholePage(string kind, bool shown)
    {
        // Arrange (Given)
        IndexModel page = CreatePage(new StubUserService(), Enum.Parse<HtmxRequest>(kind));
        page.TempData["ToastMessage"] = "Casey Mockup deleted.";

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            shown ? "Casey Mockup deleted." : null,
            page.Toast?.Message,
            "Only the whole page should show the toast.");
        Assert.IsNull(
            page.Toast?.UndoAction,
            "A delete cannot be undone, so the toast should offer no Undo.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the page as the framework would for one request: Demo User signed in, the headers HTMX 4 sends for that
    /// kind of request, the page named in the route, and a stand-in for TempData.
    /// </summary>
    /// <param name="service">The user service the page reads through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static IndexModel CreatePage(StubUserService service, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Users/Index");
        return new IndexModel(service)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
        };
    }

    #endregion Private Methods
}
