namespace HTMX.Test.Unit.Application.Web.Pages.Organizations;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages.Organizations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="ContactsModel"/> class: which page of contacts the
/// tab reads, what an action on a row sends, and what it answers, with HTMX and without it, always from the same page
/// of the list.
/// </summary>
[TestClass]
public sealed class ContactsModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the tab reads the organization's contacts, ten to a page, from the cursor in the query
    /// string.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_SecondPage_ReadsTheContactsAfterTheCursor()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(CreateContacts());
        ContactsModel page = CreatePage(contacts);
        ContactCursor cursor = new ContactCursor(new ContactId(10017), CursorPosition.After);

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        ContactCollectionRequest? sent = contacts.LastRequest();

        // Assert (Then)
        Assert.AreEqual(
            (new OrganizationId(10003), cursor, new PageSize(10)),
            (sent?.Organization, sent?.Cursor, sent?.PageSize),
            "The tab should read Placeholder Logistics's contacts after the cursor's, ten to a page.");
    }

    /// <summary>
    /// Unit test to verify that another page that HTMX asks for gets the tab's content alone: the header too, whose
    /// forms carry where the list is, but not the rest of the page.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_PageByHtmx_AnswersTheTabsContent()
    {
        // Arrange (Given)
        ContactsModel page = CreatePage(new StubContactService(CreateContacts()), HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get part of the page to swap in place.");
        Assert.AreEqual(
            "_OrganizationContacts",
            partial.ViewName,
            "The part should be the tab's whole content, header and list.");
    }

    /// <summary>
    /// Unit test to verify that the tab shows the toast a deleted contact's page left in TempData, whether that page
    /// redirected the browser here or asked HTMX to load the tab.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.None), DisplayName = "The whole page, after a redirect")]
    [DataRow(nameof(HtmxRequest.HistoryRestore), DisplayName = "The whole page, loaded by HTMX")]
    public async Task OnGetAsync_ToastLeftByADelete_ShowsItWithoutUndo(string kind)
    {
        // Arrange (Given)
        ContactsModel page = CreatePage(new StubContactService(CreateContacts()), Enum.Parse<HtmxRequest>(kind));
        page.TempData["ToastMessage"] = "Darcy Dataset deleted.";

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ("Darcy Dataset deleted.", null),
            (page.Toast?.Message, page.Toast?.UndoAction),
            "The tab should show the toast, with nothing to undo.");
    }

    /// <summary>
    /// Unit test to verify that an archive HTMX sent from a row asks for that contact among the organization's, as the
    /// signed-in user, and answers with the tab's whole content, whose header carries the page too, and a toast whose
    /// Undo restores them from the same page.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveContactAsync_ArchivedByHtmx_AnswersTheTabAndAToastWithUndo()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(CreateContacts());
        ContactsModel page = CreatePage(contacts, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page
            .OnPostArchiveContactAsync(10018, CancellationToken.None)
            .ConfigureAwait(false);
        (ChangeKind Change, ContactStatusRequest Request)? sent = contacts.LastStatusChange();

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the tab's content to swap in place.");
        Assert.AreEqual(
            "_RecordChanged",
            partial.ViewName,
            "The answer should hold the tab's content and the toast.");
        Assert.AreEqual(
            (ChangeKind.Archived, new ContactId(10018), new OrganizationId(10003), new UserId(10001)),
            (sent?.Change, sent?.Request.Id, sent?.Request.Organization, sent?.Request.ChangedBy),
            "The page should archive Florian Fallback among Placeholder Logistics's contacts, as the signed-in user.");
        Assert.AreEqual(
            "Florian Fallback archived.",
            page.Toast?.Message,
            "The toast should say who was archived.");
        Assert.AreEqual(
            "/Organizations/Contacts/10003?handler=RestoreContact&after=10017&contact=10018",
            page.Toast?.UndoAction,
            "Undo should restore them from the same page.");
        Assert.AreEqual(
            ("#organization", (ContactId?)new ContactId(10018)),
            (page.Toast?.UndoTarget, page.Focused),
            "Undo should replace the tab's content again, and the row's menu button should take the focus.");
    }

    /// <summary>
    /// Unit test to verify that a row's Undo, pressed from the toast after the list has moved to the next page, names
    /// the page it draws, the one the action ran on, for the address bar.
    /// </summary>
    [TestMethod]
    public async Task OnPostRestoreContactAsync_UndoAfterNext_NamesThePageItDraws()
    {
        // Arrange (Given)
        ContactsModel page = CreatePage(new StubContactService(CreateContacts()), HtmxRequest.Partial);
        page.Paging = new ContactsModel.PagingInputModel();

        // Act (When)
        await page.OnPostRestoreContactAsync(10018, CancellationToken.None).ConfigureAwait(false);
        IHeaderDictionary headers = page.Response.Headers;

        // Assert (Then)
        Assert.AreEqual(
            "/Organizations/Contacts/10003",
            headers["HX-Replace-Url"].ToString(),
            "The answer should name the first page, where the archive ran, not the page the address bar shows.");
    }

    /// <summary>
    /// Unit test to verify that a restore sent without HTMX is redirected back to the same page of the list, and leaves
    /// its toast, and what its Undo replaces, in TempData.
    /// </summary>
    [TestMethod]
    public async Task OnPostRestoreContactAsync_RestoredWithoutHtmx_RedirectsToTheSamePage()
    {
        // Arrange (Given)
        ContactsModel page = CreatePage(new StubContactService(CreateContacts()));

        // Act (When)
        IActionResult result = await page
            .OnPostRestoreContactAsync(10018, CancellationToken.None)
            .ConfigureAwait(false);

        // Assert (Then)
        RedirectToPageResult redirect = Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "A restore without HTMX should be redirected.");
        Assert.AreEqual(
            ("./Contacts", "10017"),
            (redirect.PageName, redirect.RouteValues?["after"]),
            "The redirect should go back to the same page of the Contacts tab.");
        Assert.AreEqual(
            ("Florian Fallback restored.", "#organization"),
            (page.TempData["ToastMessage"], page.TempData["ToastUndoTarget"]),
            "The toast, and the tab's content its Undo replaces, should wait in TempData.");
    }

    /// <summary>
    /// Unit test to verify that an archive that changed nothing says why, with no Undo.
    /// </summary>
    /// <param name="outcome">What the service answered.</param>
    /// <param name="expected">The toast's message.</param>
    [TestMethod]
    [DataRow(
        UpdateOutcome.Archived,
        "Placeholder Logistics is archived, so its contacts can't be changed until it is restored.",
        DisplayName = "Organization archived")]
    [DataRow(
        UpdateOutcome.NotFound,
        "That contact no longer belongs to Placeholder Logistics.",
        DisplayName = "Not one of its contacts")]
    [DataRow(
        UpdateOutcome.Unchanged,
        "Florian Fallback was already archived.",
        DisplayName = "Already archived")]
    public async Task OnPostArchiveContactAsync_NothingChanged_SaysWhyWithoutUndo(
        UpdateOutcome outcome,
        string expected)
    {
        // Arrange (Given)
        ContactsModel page = CreatePage(new StubContactService(CreateContacts(), outcome), HtmxRequest.Partial);

        // Act (When)
        await page.OnPostArchiveContactAsync(10018, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (expected, (string?)null),
            (page.Toast?.Message, page.Toast?.UndoAction),
            "The toast should say why nothing changed, with nothing to undo.");
    }

    /// <summary>
    /// Unit test to verify that an identifier that cannot be a contact's is refused with 400, and nothing is sent.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveContactAsync_ImpossibleIdentifier_AnswersBadRequest()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(CreateContacts());
        ContactsModel page = CreatePage(contacts, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostArchiveContactAsync(0, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<BadRequestResult>(
            result,
            "A contact identifier below 1 should be refused.");
        Assert.IsNull(
            contacts.LastStatusChange(),
            "Nothing should be sent.");
    }

    /// <summary>
    /// Unit test to verify that archiving the organization from the second page of its contacts keeps that page: its
    /// Undo posts from it, as the header's forms do.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveAsync_OrganizationFromTheSecondPage_UndoKeepsThePage()
    {
        // Arrange (Given)
        ContactsModel page = CreatePage(new StubContactService(CreateContacts()), HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostArchiveAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the tab's content to swap in place.");
        Assert.AreEqual(
            ("_RecordChanged", "/Organizations/Contacts/10003?handler=Restore&after=10017", "#organization"),
            (partial.ViewName, page.Toast?.UndoAction, page.Toast?.UndoTarget),
            "Undo should restore the organization from the same page, and replace the tab's whole content.");
    }

    /// <summary>
    /// Unit test to verify that a delete HTMX sent from a row's confirm dialog asks for that contact among the
    /// organization's, at the version the dialog showed, and answers with the tab's whole content, whose label counts
    /// the contacts, holding the same page of the list without them, and a toast with no Undo.
    /// </summary>
    [TestMethod]
    public async Task OnPostDeleteContactAsync_DeletedByHtmx_AnswersTheListWithoutThem()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(CreateContacts());
        ContactsModel page = CreatePage(contacts, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page
            .OnPostDeleteContactAsync(10018, 3, CancellationToken.None)
            .ConfigureAwait(false);
        ContactDeleteRequest? sent = contacts.LastDeletion();
        ContactCollectionRequest? readAgain = contacts.LastRequest();
        ContactSummary[] shown = page.Contacts?.Items ?? [];

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the tab's content to swap in place.");
        Assert.AreEqual(
            (new ContactId(10018), (OrganizationId?)new OrganizationId(10003), new VersionNumber(3)),
            (sent?.Id, sent?.Organization, sent?.Version),
            "The page should delete Florian Fallback among Placeholder Logistics's contacts, at the dialog's version.");
        Assert.AreEqual(
            (
                "_RecordChanged",
                (ContactCursor?)new ContactCursor(new ContactId(10017), CursorPosition.After),
                new ContactId(10019)),
            (partial.ViewName, readAgain?.Cursor, shown.Single().Id),
            "The list should be read again from the same cursor, without the contact deleted.");
        Assert.AreEqual(
            ("Florian Fallback deleted.", (string?)null),
            (page.Toast?.Message, page.Toast?.UndoAction),
            "The toast should say who was deleted, with nothing to undo.");
    }

    /// <summary>
    /// Unit test to verify that once a delete has taken a row away, the keyboard goes to the row now in its place, or
    /// to the last row when the deleted one was last.
    /// </summary>
    /// <param name="deleted">The contact deleted.</param>
    /// <param name="focused">The contact whose menu button should take the focus.</param>
    [TestMethod]
    [DataRow(10018L, 10019L, DisplayName = "The first row: the next one moves up")]
    [DataRow(10019L, 10018L, DisplayName = "The last row: the one before")]
    public async Task OnPostDeleteContactAsync_Deleted_FocusesTheRowNowInItsPlace(long deleted, long focused)
    {
        // Arrange (Given)
        ContactsModel page = CreatePage(new StubContactService(CreateContacts()), HtmxRequest.Partial);

        // Act (When)
        await page.OnPostDeleteContactAsync(deleted, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ((ContactId?)new ContactId(focused), false),
            (page.Focused, page.ListHeadingFocused),
            "The row now in the deleted one's place should take the focus.");
    }

    /// <summary>
    /// Unit test to verify that a delete that leaves no row puts the keyboard on the list's heading.
    /// </summary>
    [TestMethod]
    public async Task OnPostDeleteContactAsync_OnlyRowDeleted_FocusesTheHeading()
    {
        // Arrange (Given)
        ContactSummary[] one = [CreateContacts()[0]];
        ContactsModel page = CreatePage(new StubContactService(one), HtmxRequest.Partial);

        // Act (When)
        await page.OnPostDeleteContactAsync(10018, 3, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ((ContactId?)null, true),
            (page.Focused, page.ListHeadingFocused),
            "With no row left, the list's heading should take the focus.");
    }

    /// <summary>
    /// Unit test to verify that a delete sent without HTMX is redirected back to the same page of the list, and leaves
    /// its toast in TempData with no Undo.
    /// </summary>
    [TestMethod]
    public async Task OnPostDeleteContactAsync_DeletedWithoutHtmx_RedirectsToTheSamePage()
    {
        // Arrange (Given)
        ContactsModel page = CreatePage(new StubContactService(CreateContacts()));

        // Act (When)
        IActionResult result = await page
            .OnPostDeleteContactAsync(10018, 3, CancellationToken.None)
            .ConfigureAwait(false);

        // Assert (Then)
        RedirectToPageResult redirect = Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "A delete without HTMX should be redirected.");
        Assert.AreEqual(
            ("./Contacts", "10017"),
            (redirect.PageName, redirect.RouteValues?["after"]),
            "The redirect should go back to the same page of the Contacts tab.");
        Assert.AreEqual(
            ("Florian Fallback deleted.", null),
            (page.TempData["ToastMessage"], page.TempData["ToastUndo"]),
            "The toast should wait in TempData, with no Undo.");
    }

    /// <summary>
    /// Unit test to verify that a delete the service refuses says why, with no Undo, and keeps the keyboard on the
    /// row, which is still there.
    /// </summary>
    /// <param name="outcome">What the service answered.</param>
    /// <param name="expected">The toast's message.</param>
    [TestMethod]
    [DataRow(
        DeleteOutcome.Active,
        "Florian Fallback is active again, so they can't be deleted.",
        DisplayName = "Active")]
    [DataRow(
        DeleteOutcome.Overtaken,
        "Florian Fallback has changed since the page was shown, so they weren't deleted. Check them, then try again.",
        DisplayName = "Overtaken")]
    [DataRow(
        DeleteOutcome.NotFound,
        "That contact no longer belongs to Placeholder Logistics.",
        DisplayName = "Not one of its contacts")]
    [DataRow(
        DeleteOutcome.OrganizationArchived,
        "Placeholder Logistics is archived, so its contacts can't be changed until it is restored.",
        DisplayName = "Organization archived")]
    public async Task OnPostDeleteContactAsync_Refused_SaysWhyAndKeepsTheRow(DeleteOutcome outcome, string expected)
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(CreateContacts(), deletion: outcome);
        ContactsModel page = CreatePage(contacts, HtmxRequest.Partial);

        // Act (When)
        await page.OnPostDeleteContactAsync(10018, 3, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (expected, (string?)null, (ContactId?)new ContactId(10018)),
            (page.Toast?.Message, page.Toast?.UndoAction, page.Focused),
            "The toast should say why nothing was deleted, and the row's menu button should take the focus.");
    }

    /// <summary>
    /// Unit test to verify that an identifier or a version number that cannot be one is refused with 400, and nothing
    /// is deleted.
    /// </summary>
    /// <param name="contact">The contact's identifier, as sent.</param>
    /// <param name="version">The version number, as sent.</param>
    [TestMethod]
    [DataRow(0L, 3, DisplayName = "An impossible identifier")]
    [DataRow(10018L, 0, DisplayName = "An impossible version")]
    public async Task OnPostDeleteContactAsync_ImpossibleValues_AnswersBadRequest(long contact, int version)
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(CreateContacts());
        ContactsModel page = CreatePage(contacts, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page
            .OnPostDeleteContactAsync(contact, version, CancellationToken.None)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<BadRequestResult>(
            result,
            "An identifier or a version number below 1 should be refused.");
        Assert.IsNull(
            contacts.LastDeletion(),
            "Nothing should be deleted.");
    }

    /// <summary>
    /// Unit test to verify that another page of the list that HTMX asks for puts the keyboard on the page's heading,
    /// which the tab's content holds, and that a page the browser loads on its own leaves the focus where it starts.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="focused">Whether the heading should take the focus.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Partial), true, DisplayName = "Another page, asked for by HTMX")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The whole page, loaded by the browser")]
    public async Task OnGetAsync_PageForHtmx_FocusesTheHeading(string kind, bool focused)
    {
        // Arrange (Given)
        ContactsModel page = CreatePage(new StubContactService(CreateContacts()), Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            focused,
            page.HeadingFocused,
            "Only a page that HTMX asked for should put the keyboard on its heading.");
    }

    /// <summary>
    /// Unit test to verify that an action on a row leaves the page's heading alone: its answer puts the keyboard back
    /// on the row.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveContactAsync_ByHtmx_LeavesTheHeadingAlone()
    {
        // Arrange (Given)
        ContactsModel page = CreatePage(new StubContactService(CreateContacts()), HtmxRequest.Partial);

        // Act (When)
        await page.OnPostArchiveContactAsync(10018, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsFalse(
            page.HeadingFocused,
            "An action on a row should not move the keyboard to the page's heading.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates Placeholder Logistics at version 2, as the service would answer with it, with invented values.
    /// </summary>
    /// <returns>The organization.</returns>
    private static OrganizationSingleResponse CreateOrganization()
    {
        RecordChange change = new RecordChange
        {
            On = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc),
            By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
        };
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
    /// Creates the two contacts at the top of the second page, with invented values.
    /// </summary>
    /// <returns>The contacts, in name order.</returns>
    private static ContactSummary[] CreateContacts()
    {
        return
        [
            new ContactSummary
            {
                Id = new ContactId(10018),
                Name = new DisplayName("Florian Fallback"),
                Version = new VersionNumber(3),
            },
            new ContactSummary
            {
                Id = new ContactId(10019),
                Name = new DisplayName("Fran Footnote"),
                JobTitle = new JobTitle("Template Designer"),
                Version = new VersionNumber(1),
            },
        ];
    }

    /// <summary>
    /// Creates the tab as the framework would for one request for the second page of Placeholder Logistics's contacts:
    /// Demo User signed in, the headers HTMX 4 sends for that kind of request, and stand-ins for routing and TempData.
    /// </summary>
    /// <param name="contacts">The contact service the tab reads and changes contacts through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The tab, ready for its handler to run.</returns>
    private static ContactsModel CreatePage(StubContactService contacts, HtmxRequest kind = HtmxRequest.None)
    {
        PageContext context = PageRequests.Create(kind, "/Organizations/Contacts");
        return new ContactsModel(new StubOrganizationService(CreateOrganization()), contacts)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
            Input = new OrganizationPageModel.InputModel { Id = 10003 },
            Paging = new ContactsModel.PagingInputModel { After = 10017 },
        };
    }

    #endregion Private Methods
}
