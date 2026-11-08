namespace HTMX.Test.Unit.Application.Web.Pages.Organizations;

using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages.Organizations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
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
        IndexModel page = CreatePage(new StubOrganizationService(), htmx: false, restoringHistory: false);

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "A plain request should get the whole page.");
    }

    /// <summary>
    /// Unit test to verify that a request from HTMX gets only the list's card, to swap into the page it already shows.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_HtmxRequest_ReturnsTheListPartial()
    {
        // Arrange (Given)
        IndexModel page = CreatePage(new StubOrganizationService(), htmx: true, restoringHistory: false);

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        PartialViewResult? partial = result as PartialViewResult;

        // Assert (Then)
        Assert.IsNotNull(
            partial,
            "An HTMX request should get a partial view.");
        Assert.AreEqual(
            "_OrganizationList",
            partial.ViewName,
            "The partial should be the list's card.");
    }

    /// <summary>
    /// Unit test to verify that HTMX restoring a page for the back button, which it no longer holds, gets the whole
    /// page rather than a card with nothing around it.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_HistoryRestore_ReturnsThePage()
    {
        // Arrange (Given)
        IndexModel page = CreatePage(new StubOrganizationService(), htmx: true, restoringHistory: true);

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "A history restore should get the whole page.");
    }

    /// <summary>
    /// Unit test to verify that the response says it varies by the HX-Request header, so a cache never serves the card
    /// alone to a browser that asked for the page.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_AnyRequest_VariesByHtmxRequestHeader()
    {
        // Arrange (Given)
        IndexModel page = CreatePage(new StubOrganizationService(), htmx: true, restoringHistory: false);

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        IHeaderDictionary responseHeaders = page.Response.Headers;
        string vary = responseHeaders.Vary.ToString();

        // Assert (Then)
        Assert.AreEqual(
            "HX-Request",
            vary,
            "The response should vary by HX-Request.");
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
        IndexModel page = CreatePage(service, htmx: false, restoringHistory: false);
        page.Input = new IndexModel.InputModel
        {
            Filter = "   ",
            Status = "deleted",
            Sort = "7",
            Direction = "sideways",
            PageNumber = -3,
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
        Assert.AreEqual(
            1,
            request.Page.Value,
            "A page below 1 should fall back to 1.");
        Assert.AreEqual(
            10,
            request.PageSize.Value,
            "A page size the select does not offer should fall back to 10.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the page as the framework would for one request, with or without HTMX's headers.
    /// </summary>
    /// <param name="service">The organization service the page reads through.</param>
    /// <param name="htmx">Whether HTMX sent the request.</param>
    /// <param name="restoringHistory">Whether HTMX is restoring a page for the back button.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static IndexModel CreatePage(StubOrganizationService service, bool htmx, bool restoringHistory)
    {
        DefaultHttpContext http = new DefaultHttpContext();
        IHeaderDictionary headers = http.Request.Headers;
        if (htmx)
        {
            headers["HX-Request"] = "true";
        }

        if (restoringHistory)
        {
            headers["HX-History-Restore-Request"] = "true";
        }

        EmptyModelMetadataProvider metadata = new EmptyModelMetadataProvider();
        ActionContext action = new ActionContext(http, new RouteData(), new CompiledPageActionDescriptor());
        PageContext context = new PageContext(action)
        {
            ViewData = new ViewDataDictionary(metadata, new ModelStateDictionary()),
        };
        return new IndexModel(service)
        {
            PageContext = context,
            MetadataProvider = metadata,
        };
    }

    #endregion Private Methods
}
