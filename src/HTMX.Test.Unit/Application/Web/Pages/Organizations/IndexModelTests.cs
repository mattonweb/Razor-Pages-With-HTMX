namespace HTMX.Test.Unit.Application.Web.Pages.Organizations;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages;
using HTMX.Application.Web.Pages.Organizations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="IndexModel"/> class: which response the list page
/// sends to which request, and how it reads its query string.
/// </summary>
[TestClass]
public sealed class IndexModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a request from the browser itself, without HTMX, gets the whole page.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_PlainRequest_ReturnsThePage()
    {
        // Arrange (Given)
        IndexModel page = CreatePage(new StubOrganizationService(), HtmxRequest.None);

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "A plain request should get the whole page.");
    }

    /// <summary>
    /// Unit test to verify that a request from HTMX to swap part of the page gets only the list's card.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_PartialRequest_ReturnsTheListPartial()
    {
        // Arrange (Given)
        IndexModel page = CreatePage(new StubOrganizationService(), HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        PartialViewResult? partial = result as PartialViewResult;

        // Assert (Then)
        Assert.IsNotNull(
            partial,
            "A partial HTMX request should get a partial view.");
        Assert.AreEqual(
            "_OrganizationList",
            partial.ViewName,
            "The partial should be the list's card.");
    }

    /// <summary>
    /// Unit test to verify that HTMX restoring a page for the back button, which swaps the whole body, gets the whole
    /// page rather than a card with nothing around it.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_HistoryRestore_ReturnsThePage()
    {
        // Arrange (Given)
        IndexModel page = CreatePage(new StubOrganizationService(), HtmxRequest.HistoryRestore);

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "A history restore should get the whole page.");
    }

    /// <summary>
    /// Unit test to verify that the response says it varies by the HX-Request-Type header, so a cache never serves the
    /// card alone to a browser that asked for the page.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_AnyRequest_VariesByRequestTypeHeader()
    {
        // Arrange (Given)
        IndexModel page = CreatePage(new StubOrganizationService(), HtmxRequest.Partial);

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        IHeaderDictionary responseHeaders = page.Response.Headers;
        string vary = responseHeaders.Vary.ToString();

        // Assert (Then)
        Assert.AreEqual(
            "HX-Request-Type",
            vary,
            "The response should vary by HX-Request-Type.");
    }

    /// <summary>
    /// Unit test to verify that values the page does not recognize fall back to the defaults, so a mistyped URL still
    /// shows a list.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_UnknownValues_AsksForTheDefaults()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService();
        IndexModel page = CreatePage(service, HtmxRequest.None);
        page.Input = new ListInput
        {
            Filter = "   ",
            Status = "deleted",
            Sort = "7",
            Direction = "sideways",
            PageSize = 33,
        };

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        OrganizationCollectionRequest? request = service.LastRequest();

        // Assert (Then)
        Assert.IsNotNull(
            request,
            "The page should ask the service for a list.");
        Assert.IsNull(
            request.Filter,
            "A filter of only spaces should be no filter.");
        Assert.AreEqual(
            RecordStatus.Active,
            request.Status,
            "An unknown status should fall back to Active.");
        Assert.AreEqual(
            OrganizationSortColumn.Name,
            request.Sort,
            "A number that names no column should fall back to Name.");
        Assert.AreEqual(
            SortDirection.Ascending,
            request.Direction,
            "An unknown direction should fall back to Ascending.");
        Assert.IsNull(
            request.Cursor,
            "With neither after nor before, the page should ask for the first page.");
        Assert.AreEqual(
            10,
            request.PageSize.Value,
            "A page size the select does not offer should fall back to 10.");
    }

    /// <summary>
    /// Unit test to verify that an organization named in the query string's after value becomes the cursor the page
    /// asks the service for, as the Next link needs.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_AfterValue_AsksForThePageAfterIt()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService();
        IndexModel page = CreatePage(service, HtmxRequest.Partial);
        page.Input = new ListInput
        {
            After = 10100,
        };
        OrganizationCursor expected = new OrganizationCursor(new OrganizationId(10100), CursorPosition.After);

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        OrganizationCollectionRequest? request = service.LastRequest();

        // Assert (Then)
        Assert.IsNotNull(
            request,
            "The page should ask the service for a list.");
        Assert.AreEqual(
            expected,
            request.Cursor,
            "The page should ask for the organizations after 10100.");
    }

    /// <summary>
    /// Unit test to verify that the links the page writes use the short query string names: dir, after and size.
    /// </summary>
    [TestMethod]
    public async Task RouteValues_CursorAndChoices_UsesShortQueryNames()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService();
        IndexModel page = CreatePage(service, HtmxRequest.None);
        page.Input = new ListInput
        {
            Direction = "descending",
            PageSize = 25,
        };
        OrganizationCursor next = new OrganizationCursor(new OrganizationId(10100), CursorPosition.After);
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Act (When)
        Dictionary<string, string> values = page.RouteValues(cursor: next);

        // Assert (Then)
        CollectionAssert.AreEquivalent(
            new[] { "dir", "after", "size" },
            values.Keys,
            "Only the values that differ from the defaults should be written, under their short names.");
        Assert.AreEqual(
            "descending",
            values["dir"],
            "The direction should be written as dir.");
        Assert.AreEqual(
            "10100",
            values["after"],
            "The cursor should be written as after.");
        Assert.AreEqual(
            "25",
            values["size"],
            "The page size should be written as size.");
    }

    /// <summary>
    /// Unit test to verify that the whole page shows the toast a deleted organization's page left in TempData, and
    /// that a swap of the list's card leaves it waiting.
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
        IndexModel page = CreatePage(new StubOrganizationService(), Enum.Parse<HtmxRequest>(kind));
        page.TempData["ToastMessage"] = "Draft Distribution deleted.";

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            shown ? "Draft Distribution deleted." : null,
            page.Toast?.Message,
            "Only the whole page should show the toast.");
    }

    /// <summary>
    /// Unit test to verify that the page's heading takes the focus when HTMX asked for the whole page, as Previous and
    /// Next do to take its main content, and not when the browser loaded it on its own or HTMX asked for the card.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="focused">Whether the heading should take the focus.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The whole page, loaded by the browser")]
    [DataRow(nameof(HtmxRequest.HistoryRestore), true, DisplayName = "The whole page, asked for by HTMX")]
    [DataRow(nameof(HtmxRequest.Partial), false, DisplayName = "A swap of the card")]
    public async Task OnGetAsync_WholePageForHtmx_FocusesTheHeading(string kind, bool focused)
    {
        // Arrange (Given)
        IndexModel page = CreatePage(new StubOrganizationService(), Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            focused,
            page.HeadingFocused,
            "Only a whole page that HTMX asked for should put the keyboard on its heading.");
    }

    /// <summary>
    /// Unit test to verify that an archive HTMX sent from a row asks for that organization, as the signed-in user, and
    /// answers with the list's card and a toast whose Undo restores it from the same view, with the list's address
    /// for the address bar.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveAsync_ArchivedByHtmx_AnswersTheCardAndAToastWithUndo()
    {
        // Arrange (Given)
        StubOrganizationService service = CreateService(10003);
        IndexModel page = CreatePage(service, HtmxRequest.Partial);
        page.Input = new ListInput { After = 10001 };

        // Act (When)
        IActionResult result = await page.OnPostArchiveAsync(10003, CancellationToken.None).ConfigureAwait(false);
        (ChangeKind Change, OrganizationStatusRequest Request)? sent = service.LastStatusChange();
        IHeaderDictionary headers = page.Response.Headers;

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the list's card to swap in place.");
        Assert.AreEqual(
            "_OrganizationListChanged",
            partial.ViewName,
            "The answer should hold the list's card and the toast.");
        Assert.AreEqual(
            (ChangeKind.Archived, new OrganizationId(10003), new UserId(10001)),
            (sent?.Change, sent?.Request.Id, sent?.Request.ChangedBy),
            "The page should archive Placeholder Logistics as the signed-in user.");
        Assert.AreEqual(
            "Placeholder Logistics archived.",
            page.Toast?.Message,
            "The toast should say which organization was archived.");
        Assert.AreEqual(
            ("/Organizations/Index?handler=Restore&after=10001&organization=10003", "#organization-list"),
            (page.Toast?.UndoAction, page.Toast?.UndoTarget),
            "Undo should restore it from the same view, and replace the list's card again.");
        Assert.AreEqual(
            "/Organizations/Index?after=10001",
            headers["HX-Replace-Url"].ToString(),
            "The answer should name the view of the list it draws, for the address bar.");
    }

    /// <summary>
    /// Unit test to verify that a restore sent without HTMX is redirected back to the same view of the list, and leaves
    /// its toast, and what its Undo replaces, in TempData.
    /// </summary>
    [TestMethod]
    public async Task OnPostRestoreAsync_RestoredWithoutHtmx_RedirectsToTheSameView()
    {
        // Arrange (Given)
        IndexModel page = CreatePage(CreateService(10005), HtmxRequest.None);
        page.Input = new ListInput { Status = "archived", After = 10001 };

        // Act (When)
        IActionResult result = await page.OnPostRestoreAsync(10005, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        RedirectToPageResult redirect = Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "A restore without HTMX should be redirected.");
        Assert.AreEqual(
            ("./Index", "archived", "10001"),
            (redirect.PageName, redirect.RouteValues?["status"], redirect.RouteValues?["after"]),
            "The redirect should go back to the same view of the list.");
        Assert.AreEqual(
            ("Mockup Media restored.", "#organization-list"),
            (page.TempData["ToastMessage"], page.TempData["ToastUndoTarget"]),
            "The toast, and the card its Undo replaces, should wait in TempData.");
    }

    /// <summary>
    /// Unit test to verify that an archive that takes its row out of the Active view puts the keyboard on the row now
    /// in its place: the next row, which moves up, or the row before when the archived one was last.
    /// </summary>
    /// <param name="archived">The organization archived.</param>
    /// <param name="focused">The organization whose menu button should take the focus.</param>
    [TestMethod]
    [DataRow(10003L, 10004L, DisplayName = "The first row: the next one moves up")]
    [DataRow(10004L, 10003L, DisplayName = "The last row: the one before")]
    public async Task OnPostArchiveAsync_RowLeavesTheView_FocusesTheRowNowInItsPlace(long archived, long focused)
    {
        // Arrange (Given)
        IndexModel page = CreatePage(CreateService(archived), HtmxRequest.Partial);

        // Act (When)
        await page.OnPostArchiveAsync(archived, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ((OrganizationId?)new OrganizationId(focused), false),
            (page.Focused, page.EmptyFocused),
            "The row now in the archived one's place should take the focus.");
    }

    /// <summary>
    /// Unit test to verify that a restore that takes the last row out of the Archived view puts the keyboard on the
    /// empty list's heading.
    /// </summary>
    [TestMethod]
    public async Task OnPostRestoreAsync_LastRowLeavesTheView_FocusesTheEmptyHeading()
    {
        // Arrange (Given)
        IndexModel page = CreatePage(CreateService(10005), HtmxRequest.Partial);
        page.Input = new ListInput { Status = "archived" };

        // Act (When)
        await page.OnPostRestoreAsync(10005, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ((OrganizationId?)null, true),
            (page.Focused, page.EmptyFocused),
            "With no row left, the empty list's heading should take the focus.");
    }

    /// <summary>
    /// Unit test to verify that an archive in the All view, which keeps its row, puts the keyboard back on that row's
    /// menu button.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveAsync_RowStaysInTheView_FocusesItsOwnRow()
    {
        // Arrange (Given)
        IndexModel page = CreatePage(CreateService(10003), HtmxRequest.Partial);
        page.Input = new ListInput { Status = "all" };

        // Act (When)
        await page.OnPostArchiveAsync(10003, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ((OrganizationId?)new OrganizationId(10003), false),
            (page.Focused, page.EmptyFocused),
            "The archived row's menu button should take the focus.");
    }

    /// <summary>
    /// Unit test to verify that the toast's Undo of an archive, a restore sent from the toast for a row the Active view
    /// no longer shows, puts the keyboard on that row's menu button once the row is back.
    /// </summary>
    [TestMethod]
    public async Task OnPostRestoreAsync_UndoBringsTheRowBack_FocusesItsRow()
    {
        // Arrange (Given)
        IndexModel page = CreatePage(CreateService(10005), HtmxRequest.Partial);

        // Act (When)
        await page.OnPostRestoreAsync(10005, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ((OrganizationId?)new OrganizationId(10005), false),
            (page.Focused, page.EmptyFocused),
            "The row that came back should take the focus from the toast.");
    }

    /// <summary>
    /// Unit test to verify that an Undo sent from the toast for a row that is on neither the page shown nor the page
    /// read again, as after the list has moved on, takes the keyboard nowhere, so that it stays on the toast.
    /// </summary>
    [TestMethod]
    public async Task OnPostRestoreAsync_RowOnNeitherPage_LeavesTheFocusAlone()
    {
        // Arrange (Given)
        IndexModel page = CreatePage(CreateService(10009), HtmxRequest.Partial);

        // Act (When)
        await page.OnPostRestoreAsync(10009, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ((OrganizationId?)null, false),
            (page.Focused, page.EmptyFocused),
            "No row and no heading should take the focus.");
    }

    /// <summary>
    /// Unit test to verify that an archive or a restore that changed nothing says why, with no Undo.
    /// </summary>
    /// <param name="outcome">What the service answered.</param>
    /// <param name="expected">The toast's message.</param>
    [TestMethod]
    [DataRow(
        UpdateOutcome.Unchanged,
        "Placeholder Logistics was already archived.",
        DisplayName = "Already archived")]
    [DataRow(
        UpdateOutcome.NotFound,
        "That organization no longer exists.",
        DisplayName = "Gone")]
    [DataRow(
        UpdateOutcome.Overtaken,
        "Placeholder Logistics was changed by someone else at the same moment. Try again.",
        DisplayName = "Changed at the same moment")]
    public async Task OnPostArchiveAsync_NothingChanged_SaysWhyWithoutUndo(UpdateOutcome outcome, string expected)
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(
            CreateFound(10003),
            outcome,
            organizations: CreateOrganizations());
        IndexModel page = CreatePage(service, HtmxRequest.Partial);

        // Act (When)
        await page.OnPostArchiveAsync(10003, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (expected, (string?)null),
            (page.Toast?.Message, page.Toast?.UndoAction),
            "The toast should say why, and offer no Undo.");
    }

    /// <summary>
    /// Unit test to verify that an identifier that cannot be an organization's is refused with 400, and nothing is
    /// sent.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveAsync_ImpossibleIdentifier_AnswersBadRequest()
    {
        // Arrange (Given)
        StubOrganizationService service = CreateService(10003);
        IndexModel page = CreatePage(service, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostArchiveAsync(0, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<BadRequestResult>(
            result,
            "An organization identifier below 1 should be refused.");
        Assert.IsNull(
            service.LastStatusChange(),
            "Nothing should be sent.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a stand-in service whose list holds the three organizations, and whose lookup answers with one of them.
    /// </summary>
    /// <param name="found">The identifier of the organization a lookup answers with.</param>
    /// <returns>The service.</returns>
    private static StubOrganizationService CreateService(long found)
    {
        return new StubOrganizationService(CreateFound(found), organizations: CreateOrganizations());
    }

    /// <summary>
    /// Creates two active organizations and one archived one, with invented values, in name order within each status.
    /// </summary>
    /// <returns>The organizations: Placeholder Logistics and Sample &amp; Sons, active, and Mockup Media,
    /// archived.</returns>
    private static OrganizationSummary[] CreateOrganizations()
    {
        DateTime updated = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc);
        return
        [
            new OrganizationSummary
            {
                Id = new OrganizationId(10003),
                Name = new OrganizationName("Placeholder Logistics"),
                Industry = Industry.Shipping,
                Contacts = new RecordCount(18),
                LastModifiedOn = updated,
            },
            new OrganizationSummary
            {
                Id = new OrganizationId(10004),
                Name = new OrganizationName("Sample & Sons"),
                Industry = Industry.Retail,
                Contacts = new RecordCount(3),
                LastModifiedOn = updated,
            },
            new OrganizationSummary
            {
                Id = new OrganizationId(10005),
                Name = new OrganizationName("Mockup Media"),
                Industry = Industry.Media,
                Contacts = new RecordCount(2),
                LastModifiedOn = updated,
                ArchivedOn = updated.AddDays(-22),
            },
        ];
    }

    /// <summary>
    /// Creates one organization as a lookup would answer with it: one of the three, or another the list does not hold.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <returns>The organization, with invented values.</returns>
    private static OrganizationSingleResponse CreateFound(long id)
    {
        string name = id switch
        {
            10003 =>
                "Placeholder Logistics",
            10004 =>
                "Sample & Sons",
            10005 =>
                "Mockup Media",
            _ =>
                "Draft Distribution",
        };
        RecordChange change = new RecordChange
        {
            On = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc),
            By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
        };
        return new OrganizationSingleResponse
        {
            Id = new OrganizationId(id),
            Name = new OrganizationName(name),
            Industry = Industry.Shipping,
            Contacts = new RecordCount(0),
            Version = new VersionNumber(1),
            Created = change,
            LastModified = change,
        };
    }

    /// <summary>
    /// Creates the page as the framework would for one request: Demo User signed in, the headers HTMX 4 sends for that
    /// kind of request, and stand-ins for routing and TempData.
    /// </summary>
    /// <param name="service">The organization service the page reads through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static IndexModel CreatePage(StubOrganizationService service, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Organizations/Index");
        if (kind == HtmxRequest.HistoryRestore)
        {
            HttpRequest request = context.HttpContext.Request;
            IHeaderDictionary headers = request.Headers;
            headers["HX-History-Restore-Request"] = "true";
        }

        return new IndexModel(service)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
        };
    }

    #endregion Private Methods
}
