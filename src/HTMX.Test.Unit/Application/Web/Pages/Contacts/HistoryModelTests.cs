namespace HTMX.Test.Unit.Application.Web.Pages.Contacts;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages;
using HTMX.Application.Web.Pages.Contacts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the contacts' <see cref="HistoryModel"/> class: the timeline the
/// tab builds, what a roll-back sends, and what each outcome answers.
/// </summary>
[TestClass]
public sealed class HistoryModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the tab builds the contact's timeline, whose roll-back posts to the tab's handler and
    /// replaces #contact.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_KnownContact_BuildsTheTimeline()
    {
        // Arrange (Given)
        HistoryModel page = CreatePage(new StubContactService(found: CreateContact(), versions: CreateVersions()));

        // Act (When)
        await page.OnGetAsync(CancellationToken.None).ConfigureAwait(false);
        Timeline? timeline = page.Timeline;

        // Assert (Then)
        Assert.AreEqual(
            (2, "/Contacts/History/10001?handler=RollBack", "#contact"),
            (timeline?.Entries.Length, timeline?.RollBackAction, timeline?.Target),
            "The timeline should hold both versions, and its roll-back should post to the tab and replace #contact.");
    }

    /// <summary>
    /// Unit test to verify that a roll-back HTMX sent asks for the version it names from the version the page showed,
    /// and answers with the tab's content and a toast whose Undo rolls back again.
    /// </summary>
    [TestMethod]
    public async Task OnPostRollBackAsync_RolledBackByHtmx_AnswersTheTabAndAToastWithUndo()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(found: CreateContact(), versions: CreateVersions());
        HistoryModel page = CreatePage(contacts, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostRollBackAsync(1, 2, CancellationToken.None).ConfigureAwait(false);
        ContactRollBackRequest? sent = contacts.LastRollBack();

        // Assert (Then)
        Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the tab's content to swap in place.");
        Assert.AreEqual(
            (new VersionNumber(1), new VersionNumber(2), new UserId(10001)),
            (sent?.To, sent?.Version, sent?.ChangedBy),
            "The page should roll back to version 1 from version 2, as the signed-in user.");
        Assert.AreEqual(
            ("Ainsley Archetype rolled back to version 1.", "/Contacts/History/10001?handler=RollBack&to=2&version=3"),
            (page.Toast?.Message, page.Toast?.UndoAction),
            "The toast should say so, and Undo should roll back to version 2 from the version the roll-back made.");
    }

    /// <summary>
    /// Unit test to verify that a roll-back that changed nothing says why, in the words the timeline uses, with no
    /// Undo.
    /// </summary>
    /// <param name="outcome">What the service answered.</param>
    /// <param name="contactArchived">Whether the contact is archived.</param>
    /// <param name="expected">The toast's message.</param>
    [TestMethod]
    [DataRow(
        UpdateOutcome.OrganizationUnavailable,
        false,
        "Fictional Freight is archived, so Ainsley Archetype can't go back to version 1.",
        DisplayName = "Its organization takes no one in")]
    [DataRow(
        UpdateOutcome.Archived,
        true,
        "Ainsley Archetype is archived, so they can't be rolled back until they are restored.",
        DisplayName = "The contact is archived")]
    [DataRow(
        UpdateOutcome.Archived,
        false,
        "Placeholder Logistics is archived, so its contacts can't be changed until it is restored.",
        DisplayName = "Their organization is archived")]
    public async Task OnPostRollBackAsync_NothingChanged_SaysWhyWithoutUndo(
        UpdateOutcome outcome,
        bool contactArchived,
        string expected)
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(
            outcome: outcome,
            found: CreateContact(contactArchived),
            versions: CreateVersions());
        HistoryModel page = CreatePage(contacts, HtmxRequest.Partial);

        // Act (When)
        await page.OnPostRollBackAsync(1, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (expected, (string?)null),
            (page.Toast?.Message, page.Toast?.UndoAction),
            "The toast should say why nothing changed, with nothing to undo.");
    }

    /// <summary>
    /// Unit test to verify that a version number that cannot be one is refused with 400, and nothing is sent.
    /// </summary>
    [TestMethod]
    public async Task OnPostRollBackAsync_ImpossibleVersion_AnswersBadRequest()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(found: CreateContact());
        HistoryModel page = CreatePage(contacts, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostRollBackAsync(0, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<BadRequestResult>(
            result,
            "Version 0 should be refused.");
        Assert.IsNull(
            contacts.LastRollBack(),
            "Nothing should be sent.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates Ainsley Archetype at Placeholder Logistics, at version 2, with invented values.
    /// </summary>
    /// <param name="archived">Whether the contact is archived.</param>
    /// <returns>The contact.</returns>
    private static ContactSingleResponse CreateContact(bool archived = false)
    {
        RecordChange change = CreateChange();
        return new ContactSingleResponse
        {
            Id = new ContactId(10001),
            Name = new DisplayName("Ainsley Archetype"),
            Organization = CreatePlaceholderLogistics(),
            Version = new VersionNumber(2),
            Created = change,
            LastModified = change,
            Archived = archived ? change : null,
        };
    }

    /// <summary>
    /// Creates two versions, newest first: a move to Placeholder Logistics, and the first, at Fictional Freight, which
    /// has since been archived.
    /// </summary>
    /// <returns>The versions.</returns>
    private static ContactVersionResponse[] CreateVersions()
    {
        return
        [
            new ContactVersionResponse
            {
                Version = new VersionNumber(2),
                Change = ChangeKind.Edited,
                Changed = CreateChange(),
                Name = new DisplayName("Ainsley Archetype"),
                OrganizationId = new OrganizationId(10003),
                Organization = CreatePlaceholderLogistics(),
            },
            new ContactVersionResponse
            {
                Version = new VersionNumber(1),
                Change = ChangeKind.Created,
                Changed = CreateChange(),
                Name = new DisplayName("Ainsley Archetype"),
                OrganizationId = new OrganizationId(10002),
                Organization = new OrganizationReference
                {
                    Id = new OrganizationId(10002),
                    Name = new OrganizationName("Fictional Freight"),
                    ArchivedOn = CreateChange().On,
                },
            },
        ];
    }

    /// <summary>
    /// Creates Placeholder Logistics as a contact names it, active.
    /// </summary>
    /// <returns>The organization.</returns>
    private static OrganizationReference CreatePlaceholderLogistics()
    {
        return new OrganizationReference
        {
            Id = new OrganizationId(10003),
            Name = new OrganizationName("Placeholder Logistics"),
        };
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
    /// Creates the tab as the framework would for one request for Ainsley Archetype's history: Demo User signed in, the
    /// headers HTMX 4 sends for that kind of request, and stand-ins for routing and TempData.
    /// </summary>
    /// <param name="contacts">The contact service the tab reads and rolls back through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The tab, ready for its handler to run.</returns>
    private static HistoryModel CreatePage(StubContactService contacts, HtmxRequest kind = HtmxRequest.None)
    {
        PageContext context = PageRequests.Create(kind, "/Contacts/History");
        return new HistoryModel(contacts)
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
