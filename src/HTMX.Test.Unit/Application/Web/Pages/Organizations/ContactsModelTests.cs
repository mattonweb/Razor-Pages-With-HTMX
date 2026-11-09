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
    /// Unit test to verify that an archive HTMX sent from a row asks for that contact among the organization's, as the
    /// signed-in user, and answers with the list alone and a toast whose Undo restores them from the same page.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveContactAsync_ArchivedByHtmx_AnswersTheListAndAToastWithUndo()
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
            "HTMX should get the list to swap in place.");
        Assert.AreEqual(
            "_ContactListChanged",
            partial.ViewName,
            "The answer should hold the list and the toast, not the rest of the tab.");
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
            ("#contact-list", (ContactId?)new ContactId(10018)),
            (page.Toast?.UndoTarget, page.Focused),
            "Undo should replace the list again, and the row's menu button should take the focus.");
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
            ("Florian Fallback restored.", "#contact-list"),
            (page.TempData["ToastMessage"], page.TempData["ToastUndoTarget"]),
            "The toast, and the list its Undo replaces, should wait in TempData.");
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
            new ContactSummary { Id = new ContactId(10018), Name = new DisplayName("Florian Fallback") },
            new ContactSummary
            {
                Id = new ContactId(10019),
                Name = new DisplayName("Fran Footnote"),
                JobTitle = new JobTitle("Template Designer"),
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
