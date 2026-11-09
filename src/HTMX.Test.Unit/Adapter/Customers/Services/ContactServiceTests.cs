namespace HTMX.Test.Unit.Adapter.Customers.Services;

using System;
using System.Linq;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data.Seed;
using HTMX.Adapter.Customers.Entities;
using HTMX.Test.Unit.Adapter.Customers.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the contact service behind <see cref="IContactService"/>, over a
/// seeded in-memory database: the pages of an organization's contacts, and archiving and restoring a contact.
/// </summary>
[TestClass]
public sealed class ContactServiceTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that following Next through Placeholder Logistics's 18 contacts gives a page of ten and a
    /// page of eight, every contact once, in name order.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_FollowingNextToTheEnd_VisitsEveryContactOnceInNameOrder()
    {
        // Arrange (Given)
        ContactCollectionRequest first = ContactServiceCalls.PageOf(OrganizationSeed.PlaceholderLogistics);

        // Act (When)
        ContactCollectionResponse[] pages =
            await ContactServiceCalls.ReadInTurnAsync(first, CursorPosition.After).ConfigureAwait(false);
        string[] names = [.. pages.SelectMany(static page => page.Items).Select(static contact => contact.Name.Value)];

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { 10, 8 },
            pages.Select(static page => page.Items.Length).ToArray(),
            "The 18 contacts should come as a page of ten and a page of eight.");
        CollectionAssert.AreEqual(
            names.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            names,
            "The contacts should be in name order across both pages.");
        Assert.HasCount(
            18,
            names.Distinct(),
            "Every contact should appear once.");
        Assert.AreEqual(
            (new RecordCount(18), (ContactCursor?)null),
            (pages[^1].Total, pages[^1].Next),
            "The last page should count all 18 and have no next page.");
    }

    /// <summary>
    /// Unit test to verify that Previous from the second page gives the first page again, with no page before it.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_PreviousFromTheSecondPage_ReturnsTheFirstPage()
    {
        // Arrange (Given)
        ContactCollectionRequest first = ContactServiceCalls.PageOf(OrganizationSeed.PlaceholderLogistics);

        // Act (When)
        ContactCollectionResponse[] pages = await ContactServiceCalls
            .ReadInTurnAsync(first, CursorPosition.After, CursorPosition.Before)
            .ConfigureAwait(false);

        // Assert (Then)
        CollectionAssert.AreEqual(
            pages[0].Items.Select(static contact => contact.Id).ToArray(),
            pages[2].Items.Select(static contact => contact.Id).ToArray(),
            "Previous from the second page should give the first page's contacts, in the same order.");
        Assert.IsNull(
            pages[2].Previous,
            "The first page should have no page before it.");
    }

    /// <summary>
    /// Unit test to verify that a cursor naming another organization's contact starts again at the first page, as an
    /// old link to a contact who has since moved would.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_CursorNamesAnotherOrganizationsContact_ReturnsTheFirstPage()
    {
        // Arrange (Given)
        ContactCursor elsewhere = new ContactCursor(ContactSeed.DarcyDataset, CursorPosition.After);
        ContactCollectionRequest request = ContactServiceCalls.PageOf(OrganizationSeed.PlaceholderLogistics, elsewhere);

        // Act (When)
        ContactCollectionResponse[] pages = await ContactServiceCalls.ReadInTurnAsync(request).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNull(
            pages[0].Request.Cursor,
            "The page should say it is the first page.");
        Assert.AreEqual(
            ContactSeed.AinsleyArchetype,
            pages[0].Items[0].Id,
            "The first page should start with Ainsley Archetype, first by name.");
    }

    /// <summary>
    /// Unit test to verify that an organization nobody belongs to gives an empty page with no page either side.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_OrganizationWithoutContacts_ReturnsAnEmptyPage()
    {
        // Arrange (Given)
        ContactCollectionRequest request = ContactServiceCalls.PageOf(OrganizationSeed.DraftDistribution);

        // Act (When)
        ContactCollectionResponse[] pages = await ContactServiceCalls.ReadInTurnAsync(request).ConfigureAwait(false);
        ContactCollectionResponse page = pages[0];

        // Assert (Then)
        Assert.AreEqual(
            (0, new RecordCount(0), (ContactCursor?)null, (ContactCursor?)null),
            (page.Items.Length, page.Total, page.Previous, page.Next),
            "Draft Distribution's page should be empty, with nowhere to go.");
    }

    /// <summary>
    /// Unit test to verify that archiving an active contact saves an Archived version, keeps their values and their
    /// organization, and leaves them archived at that version.
    /// </summary>
    [TestMethod]
    public async Task ArchiveAsync_ActiveContact_ArchivesThemAsTheNextVersion()
    {
        // Arrange (Given)
        ContactStatusRequest request =
            CreateRequest(ContactSeed.AinsleyArchetype, OrganizationSeed.PlaceholderLogistics);

        // Act (When)
        (UpdateOutcome outcome, Contact? after, ContactVersion[] history) =
            await ContactServiceCalls.ChangeStatusAsync(request, ChangeKind.Archived).ConfigureAwait(false);
        ContactVersion latest = history[^1];

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.Saved,
            outcome,
            "An active contact should be archived.");
        Assert.AreEqual(
            (TestDatabase.StartedOn, (UserId?)UserSeed.DemoUser, new VersionNumber(5)),
            (after?.ArchivedOn, after?.ArchivedBy, after?.Version),
            "Ainsley Archetype should be archived now, by Demo User, at version 5.");
        Assert.AreEqual(
            (ChangeKind.Archived, OrganizationSeed.PlaceholderLogistics, "Ainsley Archetype"),
            (latest.Change, latest.OrganizationId, latest.Name.Value),
            "The new version should record the archiving and keep the contact's values.");
    }

    /// <summary>
    /// Unit test to verify that restoring an archived contact saves a Restored version and leaves them active.
    /// </summary>
    [TestMethod]
    public async Task RestoreAsync_ArchivedContact_RestoresThemAsTheNextVersion()
    {
        // Arrange (Given)
        ContactStatusRequest request = CreateRequest(ContactSeed.DarcyDataset, new OrganizationId(10005));

        // Act (When)
        (UpdateOutcome outcome, Contact? after, ContactVersion[] history) =
            await ContactServiceCalls.ChangeStatusAsync(request, ChangeKind.Restored).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.Saved,
            outcome,
            "An archived contact should be restored.");
        Assert.AreEqual(
            ((DateTime?)null, (UserId?)null, new VersionNumber(3)),
            (after?.ArchivedOn, after?.ArchivedBy, after?.Version),
            "Darcy Dataset should be active now, at version 3.");
        Assert.AreEqual(
            ChangeKind.Restored,
            history[^1].Change,
            "The new version should record the restoring.");
    }

    /// <summary>
    /// Unit test to verify that an archive or a restore that is refused, or that asks for what is already so, saves
    /// nothing and says why.
    /// </summary>
    /// <param name="contact">The contact's identifier.</param>
    /// <param name="organization">The organization the request is made among.</param>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="expected">What should become of it.</param>
    [TestMethod]
    [DataRow(10001L, 10003L, ChangeKind.Restored, UpdateOutcome.Unchanged, DisplayName = "Already active")]
    [DataRow(10004L, 10005L, ChangeKind.Archived, UpdateOutcome.Unchanged, DisplayName = "Already archived")]
    [DataRow(10002L, 10009L, ChangeKind.Archived, UpdateOutcome.Archived, DisplayName = "Organization archived")]
    [DataRow(10001L, 10002L, ChangeKind.Archived, UpdateOutcome.NotFound, DisplayName = "Moved to another")]
    [DataRow(99999L, 10003L, ChangeKind.Archived, UpdateOutcome.NotFound, DisplayName = "An unknown identifier")]
    public async Task ChangeStatus_RefusedOrAlreadyThatWay_SavesNothing(
        long contact,
        long organization,
        ChangeKind change,
        UpdateOutcome expected)
    {
        // Arrange (Given)
        ContactStatusRequest request = CreateRequest(new ContactId(contact), new OrganizationId(organization));

        // Act (When)
        (UpdateOutcome outcome, Contact? _, ContactVersion[] history) =
            await ContactServiceCalls.ChangeStatusAsync(request, change).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            outcome,
            "The request should be answered with why nothing was changed.");
        Assert.IsFalse(
            history.Any(static version => version.ChangedOn == TestDatabase.StartedOn),
            "No version should have been saved.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a request from Demo User to archive or restore a contact.
    /// </summary>
    /// <param name="contact">The contact.</param>
    /// <param name="organization">The organization the request is made among.</param>
    /// <returns>The request.</returns>
    private static ContactStatusRequest CreateRequest(ContactId contact, OrganizationId organization)
    {
        return new ContactStatusRequest
        {
            Id = contact,
            Organization = organization,
            ChangedBy = UserSeed.DemoUser,
        };
    }

    #endregion Private Methods
}
