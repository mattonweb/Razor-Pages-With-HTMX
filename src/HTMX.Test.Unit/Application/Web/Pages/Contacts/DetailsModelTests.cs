namespace HTMX.Test.Unit.Application.Web.Pages.Contacts;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages.Contacts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the contacts' <see cref="DetailsModel"/> class: which contact the
/// tab shows, what Archive sends from the contact's own page and answers, and what Delete permanently answers, with
/// HTMX and without it.
/// </summary>
[TestClass]
public sealed class DetailsModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a known contact is shown on the whole page.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_KnownContact_ShowsThem()
    {
        // Arrange (Given)
        ContactSingleResponse contact = CreateContact();
        DetailsModel page = CreatePage(new StubContactService(found: contact));

        // Act (When)
        IActionResult result = await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<PageResult>(
            result,
            "A browser should get the whole page.");
        Assert.AreSame(
            contact,
            page.Contact,
            "The page should show the contact the service read.");
    }

    /// <summary>
    /// Unit test to verify that an identifier no contact has gets the page saying so, with 404.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_UnknownContact_AnswersNotFound()
    {
        // Arrange (Given)
        DetailsModel page = CreatePage(new StubContactService());

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            StatusCodes.Status404NotFound,
            page.Response.StatusCode,
            "The page should say no contact has the identifier, with 404.");
    }

    /// <summary>
    /// Unit test to verify that Archive sent by HTMX from the contact's own page names no organization, and answers
    /// with the tab's content and a toast whose Undo restores them, replacing #contact.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveAsync_ArchivedByHtmx_AnswersTheTabAndAToastWithUndo()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(found: CreateContact());
        DetailsModel page = CreatePage(contacts, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostArchiveAsync(CancellationToken.None).ConfigureAwait(false);
        (ChangeKind Change, ContactStatusRequest Request)? sent = contacts.LastStatusChange();

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the tab's content to swap in place.");
        Assert.AreEqual(
            ("_RecordChanged", ChangeKind.Archived, (OrganizationId?)null),
            (partial.ViewName, sent?.Change, sent?.Request.Organization),
            "The page should archive the contact wherever they belong, and answer with the tab and the toast.");
        Assert.AreEqual(
            ("Ainsley Archetype archived.", "/Contacts/Details/10001?handler=Restore", "#contact"),
            (page.Toast?.Message, page.Toast?.UndoAction, page.Toast?.UndoTarget),
            "The toast's Undo should restore the contact, replacing the tab's content.");
    }

    /// <summary>
    /// Unit test to verify that Archive refused because the contact's organization is archived says so, with no Undo.
    /// </summary>
    [TestMethod]
    public async Task OnPostArchiveAsync_OrganizationArchived_SaysWhyWithoutUndo()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(outcome: UpdateOutcome.Archived, found: CreateContact());
        DetailsModel page = CreatePage(contacts, HtmxRequest.Partial);

        // Act (When)
        await page.OnPostArchiveAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (
                "Placeholder Logistics is archived, so its contacts can't be changed until it is restored.",
                (string?)null),
            (page.Toast?.Message, page.Toast?.UndoAction),
            "The toast should say why, with nothing to undo.");
    }

    /// <summary>
    /// Unit test to verify that a delete HTMX sent asks for the contact and version the dialog showed, and loads their
    /// organization's Contacts tab, leaving it a toast with no Undo.
    /// </summary>
    [TestMethod]
    public async Task OnPostDeleteAsync_DeletedByHtmx_LoadsTheContactsTabWithAToast()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(found: CreateContact());
        DetailsModel page = CreatePage(contacts, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostDeleteAsync(4, CancellationToken.None).ConfigureAwait(false);
        ContactDeleteRequest? sent = contacts.LastDeletion();

        // Assert (Then)
        StatusCodeResult status = Assert.IsInstanceOfType<StatusCodeResult>(
            result,
            "HTMX should be asked to load the Contacts tab.");
        Assert.AreEqual(
            (StatusCodes.Status204NoContent, "/Organizations/Contacts/10003"),
            (status.StatusCode, page.Response.Headers["HX-Location"].ToString()),
            "The answer should have no content to swap, and HX-Location should name the organization's Contacts tab.");
        Assert.AreEqual(
            (new ContactId(10001), new VersionNumber(4)),
            (sent?.Id, sent?.Version),
            "The page should delete the contact the route names, at the version the dialog showed.");
        Assert.AreEqual(
            ("Ainsley Archetype deleted.", null),
            (page.TempData["ToastMessage"], page.TempData["ToastUndo"]),
            "The Contacts tab should be left a toast that says so, with no Undo.");
    }

    /// <summary>
    /// Unit test to verify that a delete sent without HTMX is redirected to the contact's organization's Contacts tab.
    /// </summary>
    [TestMethod]
    public async Task OnPostDeleteAsync_DeletedWithoutHtmx_RedirectsToTheContactsTab()
    {
        // Arrange (Given)
        DetailsModel page = CreatePage(new StubContactService(found: CreateContact()));

        // Act (When)
        IActionResult result = await page.OnPostDeleteAsync(4, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        RedirectToPageResult redirect = Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "A delete without HTMX should be redirected.");
        Assert.AreEqual(
            ("/Organizations/Contacts", (object?)10003L),
            (redirect.PageName, redirect.RouteValues?["id"]),
            "The redirect should go to Placeholder Logistics's Contacts tab.");
    }

    /// <summary>
    /// Unit test to verify that a delete the service refuses answers with the tab's content and a toast that says
    /// why, with no Undo, as an archive does.
    /// </summary>
    /// <param name="outcome">What the service answers.</param>
    /// <param name="message">What the toast should say.</param>
    [TestMethod]
    [DataRow(
        DeleteOutcome.OrganizationArchived,
        "Placeholder Logistics is archived, so its contacts can't be changed until it is restored.",
        DisplayName = "Organization archived")]
    [DataRow(DeleteOutcome.Active, "Ainsley Archetype is active again, so they can't be deleted.", DisplayName = "Active")]
    [DataRow(
        DeleteOutcome.Overtaken,
        "Ainsley Archetype has changed since the page was shown, so they weren't deleted. Check them, then try again.",
        DisplayName = "Overtaken")]
    public async Task OnPostDeleteAsync_Refused_SaysWhyWithoutUndo(DeleteOutcome outcome, string message)
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(found: CreateContact(), deletion: outcome);
        DetailsModel page = CreatePage(contacts, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostDeleteAsync(4, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the tab's content to swap in place.");
        Assert.AreEqual(
            ("_RecordChanged", message, null),
            (partial.ViewName, page.Toast?.Message, page.Toast?.UndoAction),
            "The toast should say why nothing was deleted, with nothing to undo.");
    }

    /// <summary>
    /// Unit test to verify that a version number that cannot be one is refused with 400, and nothing is deleted.
    /// </summary>
    [TestMethod]
    public async Task OnPostDeleteAsync_ImpossibleVersion_AnswersBadRequest()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(found: CreateContact());
        DetailsModel page = CreatePage(contacts, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostDeleteAsync(0, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<BadRequestResult>(
            result,
            "A version number below 1 should be refused.");
        Assert.IsNull(
            contacts.LastDeletion(),
            "Nothing should be deleted.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates Ainsley Archetype at Placeholder Logistics, at version 4, with invented values.
    /// </summary>
    /// <returns>The contact.</returns>
    private static ContactSingleResponse CreateContact()
    {
        RecordChange change = new RecordChange
        {
            On = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc),
            By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
        };
        return new ContactSingleResponse
        {
            Id = new ContactId(10001),
            Name = new DisplayName("Ainsley Archetype"),
            Organization = new OrganizationReference
            {
                Id = new OrganizationId(10003),
                Name = new OrganizationName("Placeholder Logistics"),
            },
            Version = new VersionNumber(4),
            Created = change,
            LastModified = change,
        };
    }

    /// <summary>
    /// Creates the tab as the framework would for one request for Ainsley Archetype: Demo User signed in, the headers
    /// HTMX 4 sends for that kind of request, and stand-ins for routing and TempData.
    /// </summary>
    /// <param name="contacts">The contact service the tab reads and changes the contact through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The tab, ready for its handler to run.</returns>
    private static DetailsModel CreatePage(StubContactService contacts, HtmxRequest kind = HtmxRequest.None)
    {
        PageContext context = PageRequests.Create(kind, "/Contacts/Details");
        return new DetailsModel(contacts)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
            Input = new ContactPageModel.InputModel { Id = 10001 },
        };
    }

    #endregion Private Methods
}
