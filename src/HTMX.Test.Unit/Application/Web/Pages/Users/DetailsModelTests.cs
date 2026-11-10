namespace HTMX.Test.Unit.Application.Web.Pages.Users;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages;
using HTMX.Application.Web.Pages.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the users' <see cref="DetailsModel"/> class: which user the page
/// asks for, what it answers when there is none, whether it shows the signed-in user, and what Archive and Restore
/// answer, with HTMX and without it.
/// </summary>
[TestClass]
public sealed class DetailsModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the page asks for the user the route names and shows them, with the status 200.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_KnownIdentifier_ShowsTheUser()
    {
        // Arrange (Given)
        UserSingleResponse account = CreateUser(10007);
        StubUserService service = new StubUserService(account);
        DetailsModel page = CreatePage(service, 10007);

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        HttpResponse response = page.Response;

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "A known user should get the page.");
        Assert.AreEqual(
            new UserId(10007),
            service.LastLookup(),
            "The page should ask for the user the route names.");
        Assert.AreSame(
            account,
            page.Account,
            "The page should show the user the service found.");
        Assert.AreEqual(
            StatusCodes.Status200OK,
            response.StatusCode,
            "A known user should be answered with 200.");
    }

    /// <summary>
    /// Unit test to verify that an identifier that names no user still gets the page, which says so, and the status
    /// 404.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_UnknownIdentifier_AnswersNotFound()
    {
        // Arrange (Given)
        DetailsModel page = CreatePage(new StubUserService(), 99999);

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        HttpResponse response = page.Response;

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "An unknown user should still get the page, which says they were not found.");
        Assert.IsNull(
            page.Account,
            "The page should have no user to show.");
        Assert.AreEqual(
            StatusCodes.Status404NotFound,
            response.StatusCode,
            "An unknown user should be answered with 404.");
    }

    /// <summary>
    /// Unit test to verify that the page knows when it shows the signed-in user, Demo User, whose own page offers no
    /// Archive.
    /// </summary>
    /// <param name="id">The user the page shows.</param>
    /// <param name="expected">Whether that user is the one signed in.</param>
    [TestMethod]
    [DataRow(10001L, true, DisplayName = "Demo User, who is signed in")]
    [DataRow(10007L, false, DisplayName = "Pat Example, who is not")]
    public async Task IsSignedInUser_EachUser_SaysWhetherTheyAreSignedIn(long id, bool expected)
    {
        // Arrange (Given)
        DetailsModel page = CreatePage(new StubUserService(CreateUser(id)), id);

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            page.IsSignedInUser,
            "Only the signed-in user's own page should say it shows them.");
    }

    /// <summary>
    /// Unit test to verify that an archive HTMX sent answers with the page's content and a toast whose Undo restores
    /// the user, and asks for the archive as the signed-in user.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveAsync_ArchivedByHtmx_AnswersTheContentAndAToastWithUndo()
    {
        // Arrange (Given)
        StubUserService service = new StubUserService(CreateUser(10007), UpdateOutcome.Saved);
        DetailsModel page = CreatePage(service, 10007, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostArchiveAsync(CancellationToken.None).ConfigureAwait(false);
        (ChangeKind Change, UserStatusRequest Request)? asked = service.LastStatusChange();

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the page's content to swap in place.");
        Assert.AreEqual(
            "_RecordChanged",
            partial.ViewName,
            "The answer should hold the page's content and the toast.");
        Assert.AreEqual(
            (ChangeKind.Archived, new UserId(10007), new UserId(10001)),
            (asked?.Change, asked?.Request.Id, asked?.Request.ChangedBy),
            "The page should ask to archive the user the route names, as the signed-in user.");
        Assert.AreEqual(
            "Pat Example archived.",
            page.Toast?.Message,
            "The toast should say the user was archived.");
        Assert.AreEqual(
            "/Users/Details/10007?handler=Restore",
            page.Toast?.UndoAction,
            "Undo should restore the user.");
    }

    /// <summary>
    /// Unit test to verify that a restore sent without HTMX is redirected back to the page and leaves its toast in
    /// TempData for that page to show.
    /// </summary>
    [TestMethod]
    public async Task OnPostRestoreAsync_RestoredWithoutHtmx_RedirectsAndLeavesTheToast()
    {
        // Arrange (Given)
        StubUserService service = new StubUserService(CreateUser(10009), UpdateOutcome.Saved);
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
            "The redirect should go back to the user's page, with their identifier as the URL writes it.");
        Assert.AreEqual(
            "Pat Example restored.",
            page.TempData["ToastMessage"],
            "The toast should wait in TempData.");
        Assert.AreEqual(
            "/Users/Details/10009?handler=Archive",
            page.TempData["ToastUndo"],
            "Undo should archive the user again.");
    }

    /// <summary>
    /// Unit test to verify that an archive the service refuses or finds already made says why, with no Undo, because
    /// nothing was changed to undo.
    /// </summary>
    /// <param name="outcome">What the service answers.</param>
    /// <param name="message">What the toast should say.</param>
    [TestMethod]
    [DataRow(UpdateOutcome.Unchanged, "Pat Example was already archived.", DisplayName = "Already archived")]
    [DataRow(UpdateOutcome.SelfArchive, "You can't archive yourself. Another user can.", DisplayName = "Themselves")]
    public async Task OnPostArchiveAsync_NothingArchived_SaysWhyWithoutUndo(UpdateOutcome outcome, string message)
    {
        // Arrange (Given)
        DetailsModel page = CreatePage(new StubUserService(CreateUser(10007), outcome), 10007, HtmxRequest.Partial);

        // Act (When)
        await page.OnPostArchiveAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            message,
            page.Toast?.Message,
            "The toast should say why nothing changed.");
        Assert.IsNull(
            page.Toast?.UndoAction,
            "There should be nothing to undo.");
    }

    /// <summary>
    /// Unit test to verify that an archive HTMX sent for a user who no longer exists loads their page, which says so,
    /// rather than swapping an empty answer.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveAsync_UserGone_LoadsTheirPage()
    {
        // Arrange (Given)
        StubUserService service = new StubUserService(found: null, UpdateOutcome.NotFound);
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
            "/Users/Details/99999",
            page.Response.Headers["HX-Location"].ToString(),
            "HX-Location should name the user's page.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a user as the service would answer with them, with invented values.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <returns>The user.</returns>
    private static UserSingleResponse CreateUser(long id)
    {
        RecordChange change = new RecordChange
        {
            On = new DateTime(2026, 3, 8, 9, 30, 0, DateTimeKind.Utc),
            By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
        };
        return new UserSingleResponse
        {
            Id = new UserId(id),
            DisplayName = new DisplayName("Pat Example"),
            Username = new Username("pat.example"),
            Email = new EmailAddress("pat@example.com"),
            Version = new VersionNumber(1),
            Created = change,
            LastModified = change,
        };
    }

    /// <summary>
    /// Creates the page as the framework would for one request, with the identifier the route gives: Demo User signed
    /// in, the headers HTMX 4 sends for that kind of request, and stand-ins for routing and TempData.
    /// </summary>
    /// <param name="service">The user service the page reads through.</param>
    /// <param name="id">The identifier in the route.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static DetailsModel CreatePage(StubUserService service, long id, HtmxRequest kind = HtmxRequest.None)
    {
        PageContext context = PageRequests.Create(kind, "/Users/Details");
        return new DetailsModel(service)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
            Input = new RecordPageModel.InputModel { Id = id },
        };
    }

    #endregion Private Methods
}
