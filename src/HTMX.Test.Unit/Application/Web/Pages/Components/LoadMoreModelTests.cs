namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="LoadMoreModel"/> class: the Load more page, whose
/// address says how many rows show and whose Load more adds the next ten.
/// </summary>
[TestClass]
public sealed class LoadMoreModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the whole page reads as many rows as the address asks, from the first, never fewer than
    /// ten nor more than a hundred, and that HTMX's Load more is ignored without HTMX.
    /// </summary>
    /// <param name="shown">How many rows the address asks for, or <see langword="null"/> for none.</param>
    /// <param name="read">How many rows the page should read.</param>
    [TestMethod]
    [DataRow(null, 10, DisplayName = "No count")]
    [DataRow(30, 30, DisplayName = "Thirty")]
    [DataRow(3, 10, DisplayName = "Fewer than ten")]
    [DataRow(500, 100, DisplayName = "More than a hundred")]
    public async Task OnGetAsync_WholePage_ReadsTheRowsTheAddressAsks(int? shown, int read)
    {
        // Arrange (Given)
        StubOrganizationService organizations = new StubOrganizationService(organizations: CreateOrganizations(10));
        LoadMoreModel page = CreatePage(organizations, HtmxRequest.None);

        // Act (When)
        IActionResult result = await page.OnGetAsync(shown, after: 10005, CancellationToken.None).ConfigureAwait(false);
        OrganizationCollectionRequest? sent = organizations.LastRequest();

        // Assert (Then)
        Assert.AreEqual(
            (true, read, (OrganizationCursor?)null, RecordStatus.Active, OrganizationSortColumn.Name),
            (result is PageResult, sent?.PageSize.Value, sent?.Cursor, sent?.Status, sent?.Sort),
            "The page should read that many active organizations by name, from the first.");
    }

    /// <summary>
    /// Unit test to verify that HTMX's Load more reads the ten after the last row, answers them with the footer, the
    /// first of them taking the keyboard, and names the page's new address for the address bar.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_LoadMoreByHtmx_AnswersTheNextTenAndTheAddress()
    {
        // Arrange (Given)
        StubOrganizationService organizations = new StubOrganizationService(organizations: CreateOrganizations(10));
        LoadMoreModel page = CreatePage(organizations, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnGetAsync(20, after: 10010, CancellationToken.None).ConfigureAwait(false);
        OrganizationCollectionRequest? sent = organizations.LastRequest();
        IHeaderDictionary headers = page.Response.Headers;

        // Assert (Then)
        Assert.AreEqual(
            (new OrganizationCursor(new OrganizationId(10010), CursorPosition.After), 10),
            (sent?.Cursor, sent?.PageSize.Value),
            "Load more should read the ten after the last row.");
        Assert.AreEqual(
            ("_LoadMoreAnswer", 11, 20, true),
            ((result as PartialViewResult)?.ViewName, page.FirstPosition, page.Shown, page.FirstRowFocused),
            "HTMX should get rows 11 to 20 and the footer, the first taking the keyboard.");
        Assert.AreEqual(
            "/Components/LoadMore?shown=20",
            headers["HX-Replace-Url"].ToString(),
            "The answer should name the page's new address.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates active organizations, numbered from 10001.
    /// </summary>
    /// <param name="count">How many.</param>
    /// <returns>The organizations, as the list reads them.</returns>
    private static OrganizationSummary[] CreateOrganizations(int count)
    {
        OrganizationSummary[] organizations = new OrganizationSummary[count];
        for (int index = 0; index < count; index++)
        {
            organizations[index] = new OrganizationSummary
            {
                Id = new OrganizationId(10001 + index),
                Name = new OrganizationName($"Sample Organization {index + 1}"),
                Industry = Industry.Manufacturing,
                Contacts = new RecordCount(0),
                LastModifiedOn = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc),
            };
        }

        return organizations;
    }

    /// <summary>
    /// Creates the page as the framework would for one request: the headers HTMX 4 sends for that kind of request,
    /// and stand-ins for routing and what a partial needs to render.
    /// </summary>
    /// <param name="organizations">The organization service the page reads through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static LoadMoreModel CreatePage(StubOrganizationService organizations, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Components/LoadMore");
        return new LoadMoreModel(organizations)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
        };
    }

    #endregion Private Methods
}
