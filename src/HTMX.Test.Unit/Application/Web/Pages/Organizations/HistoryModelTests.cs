namespace HTMX.Test.Unit.Application.Web.Pages.Organizations;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages.Organizations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="HistoryModel"/> class: which versions the tab
/// reads, what a roll-back sends, and what each outcome answers, with HTMX and without it.
/// </summary>
[TestClass]
public sealed class HistoryModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the tab reads the organization's versions as well as the organization.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_KnownOrganization_ReadsItsVersions()
    {
        // Arrange (Given)
        OrganizationVersionResponse[] versions = CreateVersions();
        HistoryModel page = CreatePage(new StubOrganizationService(CreateOrganization(), versions: versions));

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreSame(
            versions,
            page.Versions,
            "The tab should show the versions the service read, as it gave them.");
    }

    /// <summary>
    /// Unit test to verify that a roll-back HTMX sent asks for the version it names from the version the page showed,
    /// as the signed-in user, and answers with the tab's content and a toast whose Undo rolls back again.
    /// </summary>
    [TestMethod]
    public async Task OnPostRollBackAsync_RolledBackByHtmx_AnswersTheContentAndAToastWithUndo()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(CreateOrganization(), versions: CreateVersions());
        HistoryModel page = CreatePage(service, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostRollBackAsync(1, 2, CancellationToken.None).ConfigureAwait(false);
        OrganizationRollBackRequest? sent = service.LastRollBack();

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the tab's content to swap in place.");
        Assert.AreEqual(
            "_OrganizationChanged",
            partial.ViewName,
            "The answer should hold the tab's content and the toast.");
        Assert.AreEqual(
            (new VersionNumber(1), new VersionNumber(2), new UserId(10001)),
            (sent?.To, sent?.Version, sent?.ChangedBy),
            "The page should roll back to version 1 from version 2, as the signed-in user.");
        Assert.AreEqual(
            "Placeholder Logistics rolled back to version 1.",
            page.Toast?.Message,
            "The toast should say what was rolled back to.");
        Assert.AreEqual(
            "/Organizations/History/10003?handler=RollBack&to=2&version=3",
            page.Toast?.UndoAction,
            "Undo should roll back to version 2 from the version the roll-back made.");
    }

    /// <summary>
    /// Unit test to verify that a roll-back sent without HTMX is redirected back to the History tab.
    /// </summary>
    [TestMethod]
    public async Task OnPostRollBackAsync_RolledBackWithoutHtmx_RedirectsToTheTab()
    {
        // Arrange (Given)
        HistoryModel page = CreatePage(new StubOrganizationService(CreateOrganization(), versions: CreateVersions()));

        // Act (When)
        IActionResult result = await page.OnPostRollBackAsync(1, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        RedirectToPageResult redirect = Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "A roll-back without HTMX should be redirected.");
        Assert.AreEqual(
            "./History",
            redirect.PageName,
            "The redirect should go back to the History tab.");
    }

    /// <summary>
    /// Unit test to verify that a roll-back refused because the earlier name has passed to another organization says
    /// which name, with no Undo.
    /// </summary>
    [TestMethod]
    public async Task OnPostRollBackAsync_NameTaken_SaysWhichName()
    {
        // Arrange (Given)
        StubOrganizationService service =
            new StubOrganizationService(CreateOrganization(), UpdateOutcome.NameTaken, versions: CreateVersions());
        HistoryModel page = CreatePage(service, HtmxRequest.Partial);

        // Act (When)
        await page.OnPostRollBackAsync(1, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            "Another organization is now called Placeholder Freight, so Placeholder Logistics can't go back to version 1.",
            page.Toast?.Message,
            "The toast should name the name that is taken.");
        Assert.IsNull(
            page.Toast?.UndoAction,
            "There should be nothing to undo.");
    }

    /// <summary>
    /// Unit test to verify that a version number that cannot be one is refused with 400, and nothing is sent.
    /// </summary>
    /// <param name="to">The version to roll back to.</param>
    /// <param name="version">The version the page showed.</param>
    [TestMethod]
    [DataRow(0, 2, DisplayName = "To version 0")]
    [DataRow(1, -1, DisplayName = "From version -1")]
    public async Task OnPostRollBackAsync_ImpossibleVersion_AnswersBadRequest(int to, int version)
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(CreateOrganization());
        HistoryModel page = CreatePage(service, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostRollBackAsync(to, version, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<BadRequestResult>(
            result,
            "A version number below 1 should be refused.");
        Assert.IsNull(
            service.LastRollBack(),
            "Nothing should be sent.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates Placeholder Logistics at version 2, as the service would answer with it, with invented values.
    /// </summary>
    /// <returns>The organization.</returns>
    private static OrganizationSingleResponse CreateOrganization()
    {
        RecordChange change = CreateChange();
        return new OrganizationSingleResponse
        {
            Id = new OrganizationId(10003),
            Name = new OrganizationName("Placeholder Logistics"),
            Industry = Industry.Shipping,
            Contacts = new RecordCount(18),
            Version = new VersionNumber(2),
            Created = change,
            LastModified = change,
        };
    }

    /// <summary>
    /// Creates two versions, newest first: a rename to Placeholder Logistics, and the first, as Placeholder Freight.
    /// </summary>
    /// <returns>The versions.</returns>
    private static OrganizationVersionResponse[] CreateVersions()
    {
        return
        [
            new OrganizationVersionResponse
            {
                Version = new VersionNumber(2),
                Change = ChangeKind.Edited,
                Changed = CreateChange(),
                Name = new OrganizationName("Placeholder Logistics"),
                Industry = Industry.Shipping,
            },
            new OrganizationVersionResponse
            {
                Version = new VersionNumber(1),
                Change = ChangeKind.Created,
                Changed = CreateChange(),
                Name = new OrganizationName("Placeholder Freight"),
                Industry = Industry.Shipping,
            },
        ];
    }

    /// <summary>
    /// Creates a change Demo User made.
    /// </summary>
    /// <returns>The change.</returns>
    private static RecordChange CreateChange()
    {
        return new RecordChange
        {
            On = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc),
            By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
        };
    }

    /// <summary>
    /// Creates the tab as the framework would for one request for Placeholder Logistics: Demo User signed in, the
    /// headers HTMX 4 sends for that kind of request, and stand-ins for routing and TempData.
    /// </summary>
    /// <param name="service">The organization service the tab reads and rolls back through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The tab, ready for its handler to run.</returns>
    private static HistoryModel CreatePage(StubOrganizationService service, HtmxRequest kind = HtmxRequest.None)
    {
        PageContext context = PageRequests.Create(kind, "/Organizations/History");
        return new HistoryModel(service)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
            Input = new OrganizationPageModel.InputModel { Id = 10003 },
        };
    }

    #endregion Private Methods
}
