namespace HTMX.Test.Unit.Application.Web.Pages.Organizations;

using System;
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
/// Contains unit tests for verifying the behavior of the <see cref="DetailsModel"/> class: which organization the page
/// asks for, what it answers when there is none, and what Archive and Restore answer, with HTMX and without it.
/// </summary>
[TestClass]
public sealed class DetailsModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the page asks for the organization the route names and shows it, with the status 200.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_KnownIdentifier_ShowsTheOrganization()
    {
        // Arrange (Given)
        OrganizationSingleResponse organization = CreateOrganization(10003);
        StubOrganizationService service = new StubOrganizationService(organization);
        DetailsModel page = CreatePage(service, 10003);

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        HttpResponse response = page.Response;

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "A known organization should get the page.");
        Assert.AreEqual(
            new OrganizationId(10003),
            service.LastLookup(),
            "The page should ask for the organization the route names.");
        Assert.AreSame(
            organization,
            page.Organization,
            "The page should show the organization the service found.");
        Assert.AreEqual(
            StatusCodes.Status200OK,
            response.StatusCode,
            "A known organization should be answered with 200.");
    }

    /// <summary>
    /// Unit test to verify that an identifier that names no organization still gets the page, which says so, and the
    /// status 404, so the browser and any crawler know the address is wrong.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_UnknownIdentifier_AnswersNotFound()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService();
        DetailsModel page = CreatePage(service, 99999);

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        HttpResponse response = page.Response;

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "An unknown organization should still get the page, which says it was not found.");
        Assert.IsNull(
            page.Organization,
            "The page should have no organization to show.");
        Assert.AreEqual(
            StatusCodes.Status404NotFound,
            response.StatusCode,
            "An unknown organization should be answered with 404.");
    }

    /// <summary>
    /// Unit test to verify that the page shows the toast an action left in TempData before the redirect, with an Undo
    /// that posts to the address it left.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_ToastLeftByAnAction_ShowsItWithUndo()
    {
        // Arrange (Given)
        DetailsModel page = CreatePage(new StubOrganizationService(CreateOrganization(10003)), 10003);
        page.TempData["ToastMessage"] = "Placeholder Logistics archived.";
        page.TempData["ToastUndo"] = "/Organizations/Details/10003?handler=Restore";

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        Toast? toast = page.Toast;

        // Assert (Then)
        Assert.IsNotNull(
            toast,
            "The page should show the toast the action left.");
        Assert.AreEqual(
            "Placeholder Logistics archived.",
            toast.Message,
            "The toast should say what the action did.");
        Assert.AreEqual(
            "/Organizations/Details/10003?handler=Restore",
            toast.UndoAction,
            "Undo should post to the handler that reverses the action.");
    }

    /// <summary>
    /// Unit test to verify that an archive HTMX sent answers with the page's content and a toast whose Undo restores
    /// the organization, and asks for the archive as the signed-in user.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveAsync_ArchivedByHtmx_AnswersTheContentAndAToastWithUndo()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(CreateOrganization(10003), UpdateOutcome.Saved);
        DetailsModel page = CreatePage(service, 10003, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostArchiveAsync(CancellationToken.None).ConfigureAwait(false);
        (ChangeKind Change, OrganizationStatusRequest Request)? asked = service.LastStatusChange();

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the page's content to swap in place.");
        Assert.AreEqual(
            "_OrganizationChanged",
            partial.ViewName,
            "The answer should hold the page's content and the toast.");
        Assert.AreEqual(
            (ChangeKind.Archived, new OrganizationId(10003), new UserId(10001)),
            (asked?.Change, asked?.Request.Id, asked?.Request.ChangedBy),
            "The page should ask to archive the organization the route names, as the signed-in user.");
        Assert.AreEqual(
            "Placeholder Logistics archived.",
            page.Toast?.Message,
            "The toast should say the organization was archived.");
        Assert.AreEqual(
            "/Organizations/Details/10003?handler=Restore",
            page.Toast?.UndoAction,
            "Undo should restore the organization.");
    }

    /// <summary>
    /// Unit test to verify that a restore sent without HTMX is redirected back to the page, so that refreshing it does
    /// not post again, and leaves its toast in TempData for that page to show.
    /// </summary>
    [TestMethod]
    public async Task OnPostRestoreAsync_RestoredWithoutHtmx_RedirectsAndLeavesTheToast()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(CreateOrganization(10009), UpdateOutcome.Saved);
        DetailsModel page = CreatePage(service, 10009);

        // Act (When)
        IActionResult result = await page.OnPostRestoreAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        RedirectToPageResult redirect = Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "A restore without HTMX should be redirected.");
        Assert.AreEqual(
            "10009",
            redirect.RouteValues?["id"],
            "The redirect should go back to the organization's page, with its identifier as the URL writes it.");
        Assert.AreEqual(
            "Placeholder Logistics restored.",
            page.TempData["ToastMessage"],
            "The toast should wait in TempData.");
        Assert.AreEqual(
            "/Organizations/Details/10009?handler=Archive",
            page.TempData["ToastUndo"],
            "Undo should archive the organization again.");
    }

    /// <summary>
    /// Unit test to verify that archiving an organization already archived says so, with no Undo, because nothing was
    /// changed to undo.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveAsync_AlreadyArchived_SaysSoWithoutUndo()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(CreateOrganization(10003), UpdateOutcome.Unchanged);
        DetailsModel page = CreatePage(service, 10003, HtmxRequest.Partial);

        // Act (When)
        await page.OnPostArchiveAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            "Placeholder Logistics was already archived.",
            page.Toast?.Message,
            "The toast should say nothing changed.");
        Assert.IsNull(
            page.Toast?.UndoAction,
            "There should be nothing to undo.");
    }

    /// <summary>
    /// Unit test to verify that an archive HTMX sent for an organization that no longer exists loads its page, which
    /// says so, rather than swapping an empty answer.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveAsync_OrganizationGone_LoadsItsPage()
    {
        // Arrange (Given)
        StubOrganizationService service = new StubOrganizationService(found: null, UpdateOutcome.NotFound);
        DetailsModel page = CreatePage(service, 99999, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostArchiveAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        StatusCodeResult status = Assert.IsInstanceOfType<StatusCodeResult>(
            result,
            "HTMX should be asked to load the page.");
        Assert.AreEqual(
            StatusCodes.Status204NoContent,
            status.StatusCode,
            "The answer should have no content to swap.");
        Assert.AreEqual(
            "/Organizations/Details/99999",
            page.Response.Headers["HX-Location"].ToString(),
            "HX-Location should name the organization's page.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates an organization as the service would answer with it, with invented values.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <returns>The organization.</returns>
    private static OrganizationSingleResponse CreateOrganization(long id)
    {
        UserReference demo = new UserReference
        {
            Id = new UserId(10001),
            Name = new DisplayName("Demo User"),
        };
        DateTime createdOn = new DateTime(2026, 3, 8, 9, 30, 0, DateTimeKind.Utc);
        return new OrganizationSingleResponse
        {
            Id = new OrganizationId(id),
            Name = new OrganizationName("Placeholder Logistics"),
            Industry = Industry.Shipping,
            Contacts = new RecordCount(18),
            Version = new VersionNumber(1),
            Created = new RecordChange { On = createdOn, By = demo },
            LastModified = new RecordChange { On = createdOn, By = demo },
        };
    }

    /// <summary>
    /// Creates the page as the framework would for one request, with the identifier the route gives: Demo User signed
    /// in, the headers HTMX 4 sends for that kind of request, and stand-ins for routing and TempData.
    /// </summary>
    /// <param name="service">The organization service the page reads through.</param>
    /// <param name="id">The identifier in the route.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static DetailsModel CreatePage(
        StubOrganizationService service,
        long id,
        HtmxRequest kind = HtmxRequest.None)
    {
        PageContext context = PageRequests.Create(kind, "/Organizations/Details");
        return new DetailsModel(service)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
            Input = new DetailsModel.InputModel { Id = id },
        };
    }

    #endregion Private Methods
}
