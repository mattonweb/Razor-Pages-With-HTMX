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
/// Contains unit tests for verifying the behavior of the <see cref="LoadOnScrollModel"/> class: the Load on scroll
/// page, whose last row asks for the next ten as it comes into view, and whose Load more adds them when pressed.
/// </summary>
[TestClass]
public sealed class LoadOnScrollModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a load the last row sent as it came into view answers the next ten with the page's own
    /// answer and new address, and that its rows take no keyboard, since the user did not ask for them.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task OnGetAsync_ScrolledLoad_AnswersTheRowsWithoutTheKeyboard()
    {
        // Arrange (Given)
        StubOrganizationService organizations = new StubOrganizationService(organizations: CreateOrganizations(10));
        LoadOnScrollModel page = CreatePage(organizations, HtmxRequest.Partial);
        page.Scrolled = true;

        // Act (When)
        IActionResult result = await page.OnGetAsync(20, after: 10010, CancellationToken.None).ConfigureAwait(false);
        IHeaderDictionary headers = page.Response.Headers;

        // Assert (Then)
        Assert.AreEqual(
            ("_LoadOnScrollAnswer", 11, 20, false),
            ((result as PartialViewResult)?.ViewName, page.FirstPosition, page.Shown, page.FirstRowFocused),
            "HTMX should get rows 11 to 20, none taking the keyboard.");
        Assert.AreEqual(
            "/Components/LoadOnScroll?shown=20",
            headers["HX-Replace-Url"].ToString(),
            "The answer should name the page's new address, without the load's own marker.");
    }

    /// <summary>
    /// Unit test to verify that Load more, pressed, answers the next ten with their first row taking the keyboard, as
    /// on the Load more page.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task OnGetAsync_PressedLoad_PutsTheKeyboardOnTheFirstNewRow()
    {
        // Arrange (Given)
        StubOrganizationService organizations = new StubOrganizationService(organizations: CreateOrganizations(10));
        LoadOnScrollModel page = CreatePage(organizations, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnGetAsync(20, after: 10010, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ("_LoadOnScrollAnswer", true),
            ((result as PartialViewResult)?.ViewName, page.FirstRowFocused),
            "The first new row should take the keyboard.");
    }

    /// <summary>
    /// Unit test to verify that when no organization follows the last row the page asks for nothing more as it
    /// scrolls, on the page and in a load's answer alike.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.None), DisplayName = "The page")]
    [DataRow(nameof(HtmxRequest.Partial), DisplayName = "A load")]
    public async Task OnGetAsync_NothingFollows_GrowsNoFurther(string kind)
    {
        // Arrange (Given)
        StubOrganizationService organizations = new StubOrganizationService(organizations: CreateOrganizations(10));
        LoadOnScrollModel page = CreatePage(organizations, Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        await page.OnGetAsync(20, after: 10010, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNull(
            page.GrowsFrom,
            "With no organization after the last row, the page should ask for nothing more.");
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
    private static LoadOnScrollModel CreatePage(StubOrganizationService organizations, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Components/LoadOnScroll");
        return new LoadOnScrollModel(organizations)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
        };
    }

    #endregion Private Methods
}
