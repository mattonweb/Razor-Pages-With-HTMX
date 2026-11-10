namespace HTMX.Test.Unit.Application.Web.Pages.Users;

using System;
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
/// Contains unit tests for verifying the behavior of the users' <see cref="HistoryModel"/> class: which versions the
/// tab reads, what a roll-back sends, and what each outcome answers, with HTMX and without it.
/// </summary>
[TestClass]
public sealed class HistoryModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the tab reads the user's versions as well as the user, and makes the timeline from
    /// them.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_KnownUser_ReadsTheirVersions()
    {
        // Arrange (Given)
        UserVersionResponse[] versions = CreateVersions();
        HistoryModel page = CreatePage(new StubUserService(CreateUser(), versions: versions));

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreSame(
            versions,
            page.Versions,
            "The tab should show the versions the service read, as it gave them.");
        Assert.AreEqual(
            ("Pat Example", "/Users/History/10007?handler=RollBack", "#user"),
            (page.Timeline?.RecordName, page.Timeline?.RollBackAction, page.Timeline?.Target),
            "The timeline should name the user, and roll back through this tab into the user's content.");
    }

    /// <summary>
    /// Unit test to verify that a roll-back HTMX sent asks for the version it names from the version the page showed,
    /// as the signed-in user, and answers with the tab's content and a toast whose Undo rolls back again.
    /// </summary>
    [TestMethod]
    public async Task OnPostRollBackAsync_RolledBackByHtmx_AnswersTheContentAndAToastWithUndo()
    {
        // Arrange (Given)
        StubUserService service = new StubUserService(CreateUser(), versions: CreateVersions());
        HistoryModel page = CreatePage(service, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostRollBackAsync(1, 2, CancellationToken.None).ConfigureAwait(false);
        UserRollBackRequest? sent = service.LastRollBack();

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the tab's content to swap in place.");
        Assert.AreEqual(
            "_RecordChanged",
            partial.ViewName,
            "The answer should hold the tab's content and the toast.");
        Assert.AreEqual(
            (new UserId(10007), new VersionNumber(1), new VersionNumber(2), new UserId(10001)),
            (sent?.Id, sent?.To, sent?.Version, sent?.ChangedBy),
            "The page should roll the user back to version 1 from version 2, as the signed-in user.");
        Assert.AreEqual(
            "Pat Example rolled back to version 1.",
            page.Toast?.Message,
            "The toast should say what was rolled back to.");
        Assert.AreEqual(
            "/Users/History/10007?handler=RollBack&to=2&version=3",
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
        HistoryModel page = CreatePage(new StubUserService(CreateUser(), versions: CreateVersions()));

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
    /// Unit test to verify that a roll-back refused because another user now has the earlier username or email
    /// address says which one, with no Undo.
    /// </summary>
    /// <param name="outcome">What the service answers.</param>
    /// <param name="message">What the toast should say.</param>
    [TestMethod]
    [DataRow(
        UpdateOutcome.NameTaken,
        "Another user now has the username pat.exampel, so Pat Example can't go back to version 1.",
        DisplayName = "The username")]
    [DataRow(
        UpdateOutcome.EmailTaken,
        "Another user now has the email address pat@placeholder.example, so Pat Example can't go back to version 1.",
        DisplayName = "The email address")]
    public async Task OnPostRollBackAsync_ValueTaken_SaysWhichValue(UpdateOutcome outcome, string message)
    {
        // Arrange (Given)
        StubUserService service = new StubUserService(CreateUser(), outcome, versions: CreateVersions());
        HistoryModel page = CreatePage(service, HtmxRequest.Partial);

        // Act (When)
        await page.OnPostRollBackAsync(1, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            message,
            page.Toast?.Message,
            "The toast should name the value another user has taken.");
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
        StubUserService service = new StubUserService(CreateUser());
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
    /// Creates Pat Example at version 2, as the service would answer with them, with invented values.
    /// </summary>
    /// <returns>The user.</returns>
    private static UserSingleResponse CreateUser()
    {
        RecordChange change = CreateChange();
        return new UserSingleResponse
        {
            Id = new UserId(10007),
            DisplayName = new DisplayName("Pat Example"),
            Username = new Username("pat.example"),
            Email = new EmailAddress("pat@example.com"),
            Version = new VersionNumber(2),
            Created = change,
            LastModified = change,
        };
    }

    /// <summary>
    /// Creates two versions, newest first: a correction of every value, and the first, with the name misspelled.
    /// </summary>
    /// <returns>The versions.</returns>
    private static UserVersionResponse[] CreateVersions()
    {
        return
        [
            new UserVersionResponse
            {
                Version = new VersionNumber(2),
                Change = ChangeKind.Edited,
                Changed = CreateChange(),
                DisplayName = new DisplayName("Pat Example"),
                Username = new Username("pat.example"),
                Email = new EmailAddress("pat@example.com"),
            },
            new UserVersionResponse
            {
                Version = new VersionNumber(1),
                Change = ChangeKind.Created,
                Changed = CreateChange(),
                DisplayName = new DisplayName("Pat Exampel"),
                Username = new Username("pat.exampel"),
                Email = new EmailAddress("pat@placeholder.example"),
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
    /// Creates the tab as the framework would for one request for Pat Example: Demo User signed in, the headers HTMX 4
    /// sends for that kind of request, and stand-ins for routing and TempData.
    /// </summary>
    /// <param name="service">The user service the tab reads and rolls back through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The tab, ready for its handler to run.</returns>
    private static HistoryModel CreatePage(StubUserService service, HtmxRequest kind = HtmxRequest.None)
    {
        PageContext context = PageRequests.Create(kind, "/Users/History");
        return new HistoryModel(service)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
            Input = new RecordPageModel.InputModel { Id = 10007 },
        };
    }

    #endregion Private Methods
}
