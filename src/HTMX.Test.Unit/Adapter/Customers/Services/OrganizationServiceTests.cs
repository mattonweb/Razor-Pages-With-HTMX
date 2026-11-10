namespace HTMX.Test.Unit.Adapter.Customers.Services;

using System;
using System.Linq;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data.Seed;
using HTMX.Adapter.Customers.Entities;
using HTMX.Adapter.Customers.Services;
using HTMX.Test.Unit.Adapter.Customers.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="OrganizationService"/> class, reached through
/// <see cref="IOrganizationService"/> over the seeded database.
/// </summary>
[TestClass]
public sealed class OrganizationServiceTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the list's first view, active organizations by name, holds a page of ten in name order
    /// however the names are capitalized, with the counts the status filter shows, and a next page but no previous.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_FirstPageOfActive_ReturnsTenInNameOrder()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = CreateRequest(RecordStatus.Active, OrganizationSortColumn.Name);

        // Act (When)
        OrganizationCollectionResponse response =
            await OrganizationServiceCalls.ListAsync(request).ConfigureAwait(false);
        string[] names = [.. response.Items.Select(static item => item.Name.Value)];
        string[] sorted = [.. names.Order(StringComparer.OrdinalIgnoreCase)];
        OrganizationCursor next = new OrganizationCursor(response.Items[^1].Id, CursorPosition.After);

        // Assert (Then)
        Assert.HasCount(
            10,
            response.Items,
            "The first page should hold ten organizations.");
        CollectionAssert.AreEqual(
            sorted,
            names,
            "The page should be in name order, ignoring capitals.");
        Assert.AreEqual(
            946,
            response.Matching.Value,
            "946 active organizations should match.");
        Assert.AreEqual(
            54,
            response.Archived.Value,
            "54 archived organizations should be counted for the status filter.");
        Assert.AreEqual(
            1000,
            response.All.Value,
            "1,000 organizations should be counted in all.");
        Assert.IsNull(
            response.Previous,
            "The first page should have no previous page.");
        Assert.AreEqual(
            next,
            response.Next,
            "The next page should start after the last organization shown.");
    }

    /// <summary>
    /// Unit test to verify that the filter finds its text anywhere in a name, however either is capitalized.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_FilterInCapitals_ReturnsOnlyNamesContainingIt()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = CreateRequest(RecordStatus.All, OrganizationSortColumn.Name, "FREIGHT");

        // Act (When)
        OrganizationCollectionResponse response =
            await OrganizationServiceCalls.ListAsync(request).ConfigureAwait(false);
        OrganizationSummary[] others =
        [
            .. response.Items.Where(static item =>
                !item.Name.Value.Contains("freight", StringComparison.OrdinalIgnoreCase)),
        ];

        // Assert (Then)
        Assert.IsTrue(
            response.Matching.Value > 1,
            "More than one organization should have Freight in its name.");
        Assert.HasCount(
            response.Matching.Value,
            response.Items,
            "Every match should fit on one page of 100.");
        Assert.HasCount(
            0,
            others,
            "Every organization listed should have freight in its name.");
    }

    /// <summary>
    /// Unit test to verify that a percent sign in the filter is taken literally, not as LIKE's wildcard that would
    /// match every name.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_FilterIsPercentSign_MatchesNothing()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = CreateRequest(RecordStatus.All, OrganizationSortColumn.Name, "%");

        // Act (When)
        OrganizationCollectionResponse response =
            await OrganizationServiceCalls.ListAsync(request).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            0,
            response.Matching.Value,
            "No organization's name contains a percent sign.");
        Assert.IsNull(
            response.Next,
            "An empty list should have no next page.");
    }

    /// <summary>
    /// Unit test to verify that the next page starts just after the last organization of the page before, so that two
    /// pages of ten, one after the other, are exactly the first page of twenty.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_NextCursor_ContinuesWhereThePageEnded()
    {
        // Arrange (Given)
        OrganizationCollectionRequest tens = CreateRequest(RecordStatus.Active, OrganizationSortColumn.Name);
        OrganizationCollectionRequest twenty = CreateRequest(
            RecordStatus.Active,
            OrganizationSortColumn.Name,
            pageSize: 20);

        // Act (When)
        OrganizationCollectionResponse[] pages =
            await OrganizationServiceCalls.ReadInTurnAsync(tens, CursorPosition.After).ConfigureAwait(false);
        OrganizationCollectionResponse whole = await OrganizationServiceCalls.ListAsync(twenty).ConfigureAwait(false);
        OrganizationId[] followed = [.. pages.SelectMany(static page => page.Items).Select(static item => item.Id)];
        OrganizationId[] expected = [.. whole.Items.Select(static item => item.Id)];

        // Assert (Then)
        CollectionAssert.AreEqual(
            expected,
            followed,
            "The first two pages of ten should be the first page of twenty, in the same order.");
        Assert.AreEqual(
            new OrganizationCursor(pages[1].Items[0].Id, CursorPosition.Before),
            pages[1].Previous,
            "The second page's previous page should end before its first organization.");
    }

    /// <summary>
    /// Unit test to verify that Previous from the second page goes back to exactly the first page, which has no
    /// previous page of its own.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_PreviousCursor_ReturnsThePageBefore()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = CreateRequest(RecordStatus.Active, OrganizationSortColumn.Name);

        // Act (When)
        OrganizationCollectionResponse[] pages = await OrganizationServiceCalls.ReadInTurnAsync(
            request,
            CursorPosition.After,
            CursorPosition.Before).ConfigureAwait(false);
        OrganizationId[] first = [.. pages[0].Items.Select(static item => item.Id)];
        OrganizationId[] back = [.. pages[2].Items.Select(static item => item.Id)];

        // Assert (Then)
        CollectionAssert.AreEqual(
            first,
            back,
            "Next and then Previous should come back to the first page, in the same order.");
        Assert.IsNull(
            pages[2].Previous,
            "The page Previous came back to should have no previous page.");
    }

    /// <summary>
    /// Unit test to verify that a page read backwards that reaches the start of the list too soon, as after a larger
    /// page size, shows the whole first page instead of a short one.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_PreviousReachesTheStartShort_ReturnsAWholeFirstPage()
    {
        // Arrange (Given)
        OrganizationCollectionRequest tens = CreateRequest(RecordStatus.Active, OrganizationSortColumn.Name);
        OrganizationCollectionResponse[] pages =
            await OrganizationServiceCalls.ReadInTurnAsync(tens, CursorPosition.After).ConfigureAwait(false);
        OrganizationCollectionRequest larger = OrganizationServiceCalls.At(
            CreateRequest(RecordStatus.Active, OrganizationSortColumn.Name, pageSize: 25),
            pages[1].Previous);

        // Act (When)
        OrganizationCollectionResponse response =
            await OrganizationServiceCalls.ListAsync(larger).ConfigureAwait(false);

        // Assert (Then)
        Assert.HasCount(
            25,
            response.Items,
            "Only ten organizations come before the cursor, so the whole first page of 25 should show.");
        Assert.IsNull(
            response.Request.Cursor,
            "The page shown should be the first page, with no cursor.");
        Assert.IsNull(
            response.Previous,
            "The first page should have no previous page.");
    }

    /// <summary>
    /// Unit test to verify that a cursor naming an organization that does not exist, as an old link might, starts
    /// again at the first page rather than showing nothing.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_CursorNamesNoOrganization_ReturnsTheFirstPage()
    {
        // Arrange (Given)
        OrganizationCursor unknown = new OrganizationCursor(new OrganizationId(99999), CursorPosition.After);
        OrganizationCollectionRequest request = OrganizationServiceCalls.At(
            CreateRequest(RecordStatus.Active, OrganizationSortColumn.Name),
            unknown);

        // Act (When)
        OrganizationCollectionResponse response =
            await OrganizationServiceCalls.ListAsync(request).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNull(
            response.Request.Cursor,
            "The page shown should be the first page, with no cursor.");
        Assert.HasCount(
            10,
            response.Items,
            "The first page should hold ten organizations.");
        Assert.IsNull(
            response.Previous,
            "The first page should have no previous page.");
    }

    /// <summary>
    /// Unit test to verify that following Next from the first page to the last visits every organization exactly once,
    /// in every sort order, including the ones where many organizations tie and the identifier has to settle the order.
    /// </summary>
    /// <param name="sort">What to sort by.</param>
    /// <param name="direction">Which way to sort.</param>
    [TestMethod]
    [DataRow(OrganizationSortColumn.Name, SortDirection.Ascending)]
    [DataRow(OrganizationSortColumn.Name, SortDirection.Descending)]
    [DataRow(OrganizationSortColumn.Industry, SortDirection.Ascending)]
    [DataRow(OrganizationSortColumn.Industry, SortDirection.Descending)]
    [DataRow(OrganizationSortColumn.Contacts, SortDirection.Ascending)]
    [DataRow(OrganizationSortColumn.Contacts, SortDirection.Descending)]
    [DataRow(OrganizationSortColumn.Updated, SortDirection.Ascending)]
    [DataRow(OrganizationSortColumn.Updated, SortDirection.Descending)]
    public async Task ListAsync_FollowingNextToTheEnd_VisitsEveryOrganizationOnce(
        OrganizationSortColumn sort,
        SortDirection direction)
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = CreateRequest(RecordStatus.All, sort, direction: direction);

        // Act (When)
        OrganizationCollectionResponse[] pages =
            await OrganizationServiceCalls.WalkToTheEndAsync(request).ConfigureAwait(false);
        OrganizationId[] visited = [.. pages.SelectMany(static page => page.Items).Select(static item => item.Id)];
        OrganizationId[] distinct = [.. visited.Distinct()];

        // Assert (Then)
        Assert.HasCount(
            10,
            pages,
            "1,000 organizations, 100 a page, should take ten pages.");
        Assert.HasCount(
            1000,
            visited,
            "Every organization should be shown on the way.");
        Assert.HasCount(
            1000,
            distinct,
            "No organization should be shown twice.");
        Assert.IsNull(
            pages[^1].Next,
            "The last page should have no next page.");
    }

    /// <summary>
    /// Unit test to verify that sorting by contacts, most first, starts with Placeholder Logistics, which has the most.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_SortByContactsDescending_StartsWithPlaceholderLogistics()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = CreateRequest(
            RecordStatus.All,
            OrganizationSortColumn.Contacts,
            direction: SortDirection.Descending,
            pageSize: 10);

        // Act (When)
        OrganizationCollectionResponse response =
            await OrganizationServiceCalls.ListAsync(request).ConfigureAwait(false);
        OrganizationSummary first = response.Items[0];

        // Assert (Then)
        Assert.AreEqual(
            OrganizationSeed.PlaceholderLogistics,
            first.Id,
            "Placeholder Logistics should come first.");
        Assert.AreEqual(
            18,
            first.Contacts.Value,
            "Placeholder Logistics should count its 18 contacts.");
    }

    /// <summary>
    /// Unit test to verify that sorting by the last change, newest first, keeps that order down the page.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_SortByUpdatedDescending_ReturnsNewestFirst()
    {
        // Arrange (Given)
        OrganizationCollectionRequest request = CreateRequest(
            RecordStatus.All,
            OrganizationSortColumn.Updated,
            direction: SortDirection.Descending,
            pageSize: 25);

        // Act (When)
        OrganizationCollectionResponse response =
            await OrganizationServiceCalls.ListAsync(request).ConfigureAwait(false);
        DateTime[] changes = [.. response.Items.Select(static item => item.LastModifiedOn)];
        DateTime[] newestFirst = [.. changes.OrderDescending()];

        // Assert (Then)
        CollectionAssert.AreEqual(
            newestFirst,
            changes,
            "The page should be in order of the last change, newest first.");
    }

    /// <summary>
    /// Unit test to verify that finding Placeholder Logistics reads every value its page shows: its industry, its 18
    /// contacts, its second version, and who created it and who changed it last.
    /// </summary>
    [TestMethod]
    public async Task FindAsync_PlaceholderLogistics_ReturnsItsValuesAndWhoChangedIt()
    {
        // Arrange (Given)
        OrganizationId id = OrganizationSeed.PlaceholderLogistics;

        // Act (When)
        OrganizationSingleResponse? organization = await OrganizationServiceCalls.FindAsync(id).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNotNull(
            organization,
            "Placeholder Logistics should be found.");
        UserReference creator = organization.Created.By;
        UserReference editor = organization.LastModified.By;
        Assert.AreEqual(
            Industry.Shipping,
            organization.Industry,
            "Placeholder Logistics should be in shipping.");
        Assert.AreEqual(
            18,
            organization.Contacts.Value,
            "Placeholder Logistics should count its 18 contacts.");
        Assert.AreEqual(
            2,
            organization.Version.Value,
            "Placeholder Logistics should be at its second version.");
        Assert.AreEqual(
            "Demo User",
            creator.Name.Value,
            "Demo User should have created Placeholder Logistics.");
        Assert.AreEqual(
            "Taylor Dummy",
            editor.Name.Value,
            "Taylor Dummy should have changed Placeholder Logistics last.");
        Assert.IsNull(
            organization.Archived,
            "Placeholder Logistics is active, so it should have no archive change.");
    }

    /// <summary>
    /// Unit test to verify that an archived organization says who archived it and when: Mockup Media, archived by Demo
    /// User as its last change.
    /// </summary>
    [TestMethod]
    public async Task FindAsync_MockupMedia_SaysWhoArchivedIt()
    {
        // Arrange (Given)
        OrganizationId id = OrganizationSeed.MockupMedia;

        // Act (When)
        OrganizationSingleResponse? organization = await OrganizationServiceCalls.FindAsync(id).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNotNull(
            organization,
            "Mockup Media should be found.");
        RecordChange? archived = organization.Archived;
        Assert.IsNotNull(
            archived,
            "Mockup Media should say it was archived.");
        UserReference archiver = archived.By;
        Assert.AreEqual(
            "Demo User",
            archiver.Name.Value,
            "Demo User should have archived Mockup Media.");
        Assert.AreEqual(
            organization.LastModified.On,
            archived.On,
            "Archiving should be Mockup Media's last change.");
    }

    /// <summary>
    /// Unit test to verify that an identifier no organization has finds nothing, rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task FindAsync_UnknownIdentifier_ReturnsNull()
    {
        // Arrange (Given)
        OrganizationId unknown = new OrganizationId(99999);

        // Act (When)
        OrganizationSingleResponse? organization =
            await OrganizationServiceCalls.FindAsync(unknown).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNull(
            organization,
            "No organization has the identifier 99999.");
    }

    /// <summary>
    /// Unit test to verify that a change from the current version is saved as the next one: the organization takes its
    /// values, says who changed it and when, and its history gains an Edited version holding the same values.
    /// </summary>
    [TestMethod]
    public async Task UpdateAsync_NewValuesFromTheCurrentVersion_SavesTheNextVersion()
    {
        // Arrange (Given)
        OrganizationId id = OrganizationSeed.PlaceholderLogistics;
        OrganizationUpdateRequest request =
            CreateUpdate(id, 2, "Placeholder Logistics Group", Industry.Technology, website: null);

        // Act (When)
        (UpdateOutcome[] outcomes, OrganizationSingleResponse? after, OrganizationVersion[] history) =
            await OrganizationServiceCalls.UpdateInTurnAsync(id, request).ConfigureAwait(false);
        OrganizationVersion latest = history[^1];

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.Saved,
            outcomes[0],
            "A change from the current version should be saved.");
        Assert.IsNotNull(
            after,
            "Placeholder Logistics should still be found.");
        Assert.AreEqual(
            "Placeholder Logistics Group",
            after.Name.Value,
            "The organization should take the new name.");
        Assert.AreEqual(
            Industry.Technology,
            after.Industry,
            "The organization should take the new industry.");
        Assert.IsNull(
            after.Website,
            "A website left empty should leave none on record.");
        Assert.AreEqual(
            new VersionNumber(3),
            after.Version,
            "The organization should move from version 2 to version 3.");
        Assert.AreEqual(
            TestDatabase.StartedOn,
            after.LastModified.On,
            "The change should be dated by the clock.");
        Assert.AreEqual(
            UserSeed.DemoUser,
            after.LastModified.By.Id,
            "The change should name who saved it.");
        Assert.HasCount(
            3,
            history,
            "The change should add one version to the two the seed wrote.");
        Assert.AreEqual(
            ChangeKind.Edited,
            latest.Change,
            "The new version should record an edit.");
        Assert.AreEqual(
            after.Name,
            latest.Name,
            "The new version should hold the values the organization now has.");
    }

    /// <summary>
    /// Unit test to verify that a change the rules refuse, or that changes nothing, saves nothing: the organization
    /// keeps its version and its history gains no version. The rules run in order, so each row breaks only one.
    /// </summary>
    /// <param name="id">The organization the change is for.</param>
    /// <param name="version">The version the change starts from.</param>
    /// <param name="name">The name the change gives.</param>
    /// <param name="expected">What should become of the change.</param>
    [TestMethod]
    [DataRow(10003L, 2, "Placeholder Logistics", UpdateOutcome.Unchanged, DisplayName = "The values it already holds")]
    [DataRow(10003L, 1, "Placeholder Logistics Group", UpdateOutcome.Overtaken, DisplayName = "From an older version")]
    [DataRow(10003L, 2, "SAMPLE & SONS", UpdateOutcome.NameTaken, DisplayName = "Another's name in capitals")]
    [DataRow(10009L, 2, "Mockup Media Group", UpdateOutcome.Archived, DisplayName = "An archived organization")]
    [DataRow(99999L, 1, "Nobody Holdings", UpdateOutcome.NotFound, DisplayName = "An unknown identifier")]
    public async Task UpdateAsync_RefusedOrUnchanged_SavesNothing(
        long id,
        int version,
        string name,
        UpdateOutcome expected)
    {
        // Arrange (Given)
        OrganizationId organizationId = new OrganizationId(id);
        OrganizationUpdateRequest request = CreateUpdate(organizationId, version, name);
        OrganizationSingleResponse? before = await OrganizationServiceCalls.FindAsync(organizationId).ConfigureAwait(false);

        // Act (When)
        (UpdateOutcome[] outcomes, OrganizationSingleResponse? after, OrganizationVersion[] history) =
            await OrganizationServiceCalls.UpdateInTurnAsync(organizationId, request).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            outcomes[0],
            $"The change should be answered {expected}.");
        Assert.AreEqual(
            before?.Version,
            after?.Version,
            "The organization should keep its version.");
        Assert.HasCount(
            before is null ? 0 : before.Version.Value,
            history,
            "The history should gain no version.");
    }

    /// <summary>
    /// Unit test to verify that an organization can be renamed to its own name in other capitals: a name counts as
    /// taken only when another organization has it.
    /// </summary>
    [TestMethod]
    public async Task UpdateAsync_OwnNameInCapitals_SavesIt()
    {
        // Arrange (Given)
        OrganizationId id = OrganizationSeed.PlaceholderLogistics;
        OrganizationUpdateRequest request = CreateUpdate(id, 2, "PLACEHOLDER LOGISTICS");

        // Act (When)
        (UpdateOutcome[] outcomes, OrganizationSingleResponse? after, _) =
            await OrganizationServiceCalls.UpdateInTurnAsync(id, request).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.Saved,
            outcomes[0],
            "The organization's own name in capitals should not count as taken.");
        Assert.AreEqual(
            "PLACEHOLDER LOGISTICS",
            after?.Name.Value,
            "The organization should take the new capitals.");
    }

    /// <summary>
    /// Unit test to verify that of two changes started from the same version, as from two browser tabs, the first is
    /// saved and the second is answered Overtaken rather than saved over it.
    /// </summary>
    [TestMethod]
    public async Task UpdateAsync_SecondChangeFromTheSameVersion_IsOvertaken()
    {
        // Arrange (Given)
        OrganizationId id = OrganizationSeed.PlaceholderLogistics;
        OrganizationUpdateRequest first = CreateUpdate(id, 2, "Placeholder Logistics Group");
        OrganizationUpdateRequest second = CreateUpdate(id, 2, "Placeholder Logistics Limited");

        // Act (When)
        (UpdateOutcome[] outcomes, OrganizationSingleResponse? after, OrganizationVersion[] history) =
            await OrganizationServiceCalls.UpdateInTurnAsync(id, first, second).ConfigureAwait(false);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { UpdateOutcome.Saved, UpdateOutcome.Overtaken },
            outcomes,
            "The first change should be saved and the second refused.");
        Assert.AreEqual(
            "Placeholder Logistics Group",
            after?.Name.Value,
            "The organization should keep the first change.");
        Assert.HasCount(
            3,
            history,
            "Only the first change should add a version.");
    }

    /// <summary>
    /// Unit test to verify that a new organization is created at the next identifier after the seed, at version 1, with
    /// no contacts, created and last changed by whoever sent it, and with one Created version holding its values.
    /// </summary>
    [TestMethod]
    public async Task CreateAsync_NewName_CreatesItWithItsFirstVersion()
    {
        // Arrange (Given)
        OrganizationId expected = new OrganizationId(11001);
        OrganizationCreateRequest request = CreateNew("Newcomer Networks");

        // Act (When)
        (OrganizationCreateResponse response, OrganizationSingleResponse? after, OrganizationVersion[] history) =
            await OrganizationServiceCalls.CreateAsync(request, expected).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            CreateOutcome.Created,
            response.Outcome,
            "A name no organization has should be created.");
        Assert.AreEqual(
            expected,
            response.Id,
            "The first organization after the seed should be 11001.");
        Assert.IsNotNull(
            after,
            "The new organization should be found at its identifier.");
        Assert.AreEqual(
            "Newcomer Networks",
            after.Name.Value,
            "The organization should hold the name sent.");
        Assert.AreEqual(
            new VersionNumber(1),
            after.Version,
            "A new organization should be at version 1.");
        Assert.AreEqual(
            0,
            after.Contacts.Value,
            "A new organization should have no contacts.");
        Assert.AreEqual(
            UserSeed.DemoUser,
            after.Created.By.Id,
            "The organization should name who created it.");
        Assert.AreEqual(
            TestDatabase.StartedOn,
            after.LastModified.On,
            "A new organization was last changed when it was created.");
        Assert.HasCount(
            1,
            history,
            "A new organization should have one version.");
        Assert.AreEqual(
            ChangeKind.Created,
            history[0].Change,
            "The first version should record the creation.");
    }

    /// <summary>
    /// Unit test to verify that a new organization with another's name, in other capitals, is not created.
    /// </summary>
    [TestMethod]
    public async Task CreateAsync_NameOfAnotherInCapitals_CreatesNothing()
    {
        // Arrange (Given)
        OrganizationId next = new OrganizationId(11001);
        OrganizationCreateRequest request = CreateNew("PLACEHOLDER LOGISTICS");

        // Act (When)
        (OrganizationCreateResponse response, OrganizationSingleResponse? after, OrganizationVersion[] history) =
            await OrganizationServiceCalls.CreateAsync(request, next).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            CreateOutcome.NameTaken,
            response.Outcome,
            "Another organization's name in capitals should be refused.");
        Assert.IsNull(
            response.Id,
            "Nothing should be given an identifier.");
        Assert.IsNull(
            after,
            "No organization should be created.");
        Assert.IsEmpty(
            history,
            "No version should be written.");
    }

    /// <summary>
    /// Unit test to verify that archiving an active organization saves an Archived version that says who archived it
    /// and when, keeps its values, and leaves the organization archived at that version.
    /// </summary>
    [TestMethod]
    public async Task ArchiveAsync_ActiveOrganization_ArchivesItAsTheNextVersion()
    {
        // Arrange (Given)
        OrganizationId id = OrganizationSeed.PlaceholderLogistics;

        // Act (When)
        (UpdateOutcome outcome, OrganizationSingleResponse? after, OrganizationVersion[] history) =
            await OrganizationServiceCalls.ChangeStatusAsync(id, ChangeKind.Archived).ConfigureAwait(false);
        OrganizationVersion latest = history[^1];

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.Saved,
            outcome,
            "An active organization should be archived.");
        Assert.IsNotNull(
            after?.Archived,
            "The organization should say it is archived.");
        Assert.AreEqual(
            UserSeed.DemoUser,
            after.Archived.By.Id,
            "The organization should say who archived it.");
        Assert.AreEqual(
            TestDatabase.StartedOn,
            after.Archived.On,
            "The organization should say when it was archived.");
        Assert.AreEqual(
            new VersionNumber(3),
            after.Version,
            "Archiving should move the organization from version 2 to version 3.");
        Assert.AreEqual(
            "Placeholder Logistics",
            after.Name.Value,
            "Archiving should keep the organization's values.");
        Assert.AreEqual(
            ChangeKind.Archived,
            latest.Change,
            "The new version should record the archiving.");
        Assert.AreEqual(
            UserSeed.DemoUser,
            latest.ArchivedBy,
            "The new version should say who archived the organization.");
    }

    /// <summary>
    /// Unit test to verify that restoring an archived organization saves a Restored version and leaves the
    /// organization active at that version.
    /// </summary>
    [TestMethod]
    public async Task RestoreAsync_ArchivedOrganization_RestoresItAsTheNextVersion()
    {
        // Arrange (Given)
        OrganizationId id = OrganizationSeed.MockupMedia;

        // Act (When)
        (UpdateOutcome outcome, OrganizationSingleResponse? after, OrganizationVersion[] history) =
            await OrganizationServiceCalls.ChangeStatusAsync(id, ChangeKind.Restored).ConfigureAwait(false);
        OrganizationVersion latest = history[^1];

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.Saved,
            outcome,
            "An archived organization should be restored.");
        Assert.IsNotNull(
            after,
            "Mockup Media should still be found.");
        Assert.IsNull(
            after.Archived,
            "The organization should be active again.");
        Assert.AreEqual(
            new VersionNumber(3),
            after.Version,
            "Restoring should move the organization from version 2 to version 3.");
        Assert.AreEqual(
            ChangeKind.Restored,
            latest.Change,
            "The new version should record the restoring.");
        Assert.IsNull(
            latest.ArchivedOn,
            "The new version should leave the organization unarchived.");
    }

    /// <summary>
    /// Unit test to verify that archiving an organization already archived, restoring one already active, or either
    /// for an unknown identifier, saves nothing.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="change">Archived to archive it, Restored to restore it.</param>
    /// <param name="expected">What should become of the request.</param>
    [TestMethod]
    [DataRow(10009L, ChangeKind.Archived, UpdateOutcome.Unchanged, DisplayName = "Archive one already archived")]
    [DataRow(10003L, ChangeKind.Restored, UpdateOutcome.Unchanged, DisplayName = "Restore one already active")]
    [DataRow(99999L, ChangeKind.Archived, UpdateOutcome.NotFound, DisplayName = "An unknown identifier")]
    public async Task ChangeStatus_AlreadyThatWayOrUnknown_SavesNothing(
        long id,
        ChangeKind change,
        UpdateOutcome expected)
    {
        // Arrange (Given)
        OrganizationId organizationId = new OrganizationId(id);
        OrganizationSingleResponse? before = await OrganizationServiceCalls.FindAsync(organizationId).ConfigureAwait(false);

        // Act (When)
        (UpdateOutcome outcome, OrganizationSingleResponse? after, OrganizationVersion[] history) =
            await OrganizationServiceCalls.ChangeStatusAsync(organizationId, change).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            outcome,
            $"The request should be answered {expected}.");
        Assert.AreEqual(
            before?.Version,
            after?.Version,
            "The organization should keep its version.");
        Assert.HasCount(
            before is null ? 0 : before.Version.Value,
            history,
            "The history should gain no version.");
    }

    /// <summary>
    /// Unit test to verify that an organization's versions come newest first, each with what happened, who did it and
    /// the values it left.
    /// </summary>
    [TestMethod]
    public async Task ListVersionsAsync_PlaceholderLogistics_ReturnsNewestFirstWithWhoMadeEach()
    {
        // Arrange (Given)
        OrganizationId id = OrganizationSeed.PlaceholderLogistics;

        // Act (When)
        OrganizationVersionResponse[] versions = await OrganizationServiceCalls.ListVersionsAsync(id).ConfigureAwait(false);

        // Assert (Then)
        Assert.HasCount(
            2,
            versions,
            "Placeholder Logistics has two versions.");
        Assert.AreEqual(
            (new VersionNumber(2), ChangeKind.Edited, "Taylor Dummy"),
            (versions[0].Version, versions[0].Change, versions[0].Changed.By.Name.Value),
            "The newest version should come first, with who made it.");
        Assert.AreEqual(
            (new VersionNumber(1), ChangeKind.Created, "Demo User"),
            (versions[1].Version, versions[1].Change, versions[1].Changed.By.Name.Value),
            "The first version should come last.");
        Assert.AreEqual(
            new WebAddress("https://placeholder-logistics.example"),
            versions[1].Website,
            "Each version should hold the values it left.");
    }

    /// <summary>
    /// Unit test to verify that rolling back saves the earlier version's values as the next version, a RolledBack one
    /// that names the version it went back to.
    /// </summary>
    [TestMethod]
    public async Task RollBackAsync_ToAnEarlierVersion_SavesItsValuesAsTheNextVersion()
    {
        // Arrange (Given)
        OrganizationRollBackRequest request = CreateRollBack(OrganizationSeed.PlaceholderLogistics, 2, 1);

        // Act (When)
        (UpdateOutcome outcome, OrganizationSingleResponse? after, OrganizationVersion[] history) =
            await OrganizationServiceCalls.RollBackAsync(request).ConfigureAwait(false);
        OrganizationVersion latest = history[^1];

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.Saved,
            outcome,
            "Rolling back to a version with other values should be saved.");
        Assert.AreEqual(
            new WebAddress("https://placeholder-logistics.example"),
            after?.Website,
            "The organization should take version 1's website again.");
        Assert.AreEqual(
            new VersionNumber(3),
            after?.Version,
            "The roll-back should be version 3, not a return to version 1.");
        Assert.AreEqual(
            (ChangeKind.RolledBack, new VersionNumber(1)),
            (latest.Change, latest.RolledBackTo),
            "The new version should record a roll-back to version 1.");
    }

    /// <summary>
    /// Unit test to verify that a roll-back the rules refuse, or that changes nothing, saves nothing.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="version">The version the request says the organization is at.</param>
    /// <param name="to">The version to roll back to.</param>
    /// <param name="expected">What should become of the request.</param>
    [TestMethod]
    [DataRow(10003L, 1, 1, UpdateOutcome.Overtaken, DisplayName = "From a version it has moved on from")]
    [DataRow(10003L, 2, 2, UpdateOutcome.Unchanged, DisplayName = "To the version it is at")]
    [DataRow(10009L, 2, 1, UpdateOutcome.Archived, DisplayName = "An archived organization")]
    [DataRow(10003L, 2, 9, UpdateOutcome.NotFound, DisplayName = "To a version it never had")]
    [DataRow(99999L, 1, 1, UpdateOutcome.NotFound, DisplayName = "An unknown identifier")]
    public async Task RollBackAsync_RefusedOrUnchanged_SavesNothing(
        long id,
        int version,
        int to,
        UpdateOutcome expected)
    {
        // Arrange (Given)
        OrganizationId organizationId = new OrganizationId(id);
        OrganizationRollBackRequest request = CreateRollBack(organizationId, version, to);
        OrganizationSingleResponse? before = await OrganizationServiceCalls.FindAsync(organizationId).ConfigureAwait(false);

        // Act (When)
        (UpdateOutcome outcome, OrganizationSingleResponse? after, _) =
            await OrganizationServiceCalls.RollBackAsync(request).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            outcome,
            $"The roll-back should be answered {expected}.");
        Assert.AreEqual(
            before?.Version,
            after?.Version,
            "The organization should keep its version.");
    }

    /// <summary>
    /// Unit test to verify that an archived organization that no contact belongs to is deleted, with its history, at
    /// the version the page showed.
    /// </summary>
    [TestMethod]
    public async Task DeleteAsync_DraftDistribution_DeletesItAndItsHistory()
    {
        // Arrange (Given)
        OrganizationDeleteRequest request = CreateDelete(OrganizationSeed.DraftDistribution, 2);

        // Act (When)
        (DeleteOutcome outcome, OrganizationSingleResponse? after, OrganizationVersion[] history, _) =
            await OrganizationServiceCalls.DeleteAsync(request, archivedFirst: false, new ContactId(10001))
                .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            DeleteOutcome.Deleted,
            outcome,
            "Draft Distribution, archived with no contacts, should be deleted.");
        Assert.IsNull(
            after,
            "Draft Distribution should no longer be found.");
        Assert.IsEmpty(
            history,
            "Its history should go with it.");
    }

    /// <summary>
    /// Unit test to verify that an organization a contact's history names, but that no contact belongs to now, can be
    /// deleted once it is archived, and that the history keeps its identifier and says it no longer exists.
    /// </summary>
    [TestMethod]
    public async Task DeleteAsync_FictionalFreightOnceArchived_LeavesAinsleysHistoryNamingIt()
    {
        // Arrange (Given)
        OrganizationDeleteRequest request = CreateDelete(new OrganizationId(10002), 2);

        // Act (When)
        (DeleteOutcome outcome, _, _, ContactVersionResponse[] ainsley) =
            await OrganizationServiceCalls.DeleteAsync(request, archivedFirst: true, new ContactId(10001))
                .ConfigureAwait(false);
        ContactVersionResponse[] atFictionalFreight =
            [.. ainsley.Where(static version => version.OrganizationId == new OrganizationId(10002))];

        // Assert (Then)
        Assert.AreEqual(
            DeleteOutcome.Deleted,
            outcome,
            "Fictional Freight, archived with no contacts, should be deleted although Ainsley's history names it.");
        Assert.IsNotEmpty(
            atFictionalFreight,
            "Ainsley's history should still name Fictional Freight by its identifier.");
        Assert.IsTrue(
            atFictionalFreight.All(static version => version.Organization is null),
            "Those versions should say the organization no longer exists.");
    }

    /// <summary>
    /// Unit test to verify that a delete the rules refuse deletes nothing. The rules run in order, so each row breaks
    /// only one.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="version">The version the page showed it at.</param>
    /// <param name="expected">What should become of the request.</param>
    [TestMethod]
    [DataRow(99999L, 1, DeleteOutcome.NotFound, DisplayName = "An unknown identifier")]
    [DataRow(10003L, 2, DeleteOutcome.Active, DisplayName = "An active organization")]
    [DataRow(10013L, 1, DeleteOutcome.Overtaken, DisplayName = "From a version it has moved on from")]
    [DataRow(10009L, 2, DeleteOutcome.InUse, DisplayName = "An archived organization with contacts")]
    public async Task DeleteAsync_Refused_DeletesNothing(long id, int version, DeleteOutcome expected)
    {
        // Arrange (Given)
        OrganizationDeleteRequest request = CreateDelete(new OrganizationId(id), version);
        OrganizationSingleResponse? before = await OrganizationServiceCalls.FindAsync(request.Id).ConfigureAwait(false);

        // Act (When)
        (DeleteOutcome outcome, OrganizationSingleResponse? after, OrganizationVersion[] history, _) =
            await OrganizationServiceCalls.DeleteAsync(request, archivedFirst: false, new ContactId(10001))
                .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            outcome,
            $"The delete should be answered {expected}.");
        Assert.AreEqual(
            before?.Version,
            after?.Version,
            "The organization should still be there, at the version it was.");
        Assert.HasCount(
            before is null ? 0 : before.Version.Value,
            history,
            "The history should keep every version.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a request for the first page: ten a page for active organizations, 100 otherwise, unless a size is
    /// given.
    /// </summary>
    /// <param name="status">Which organizations to list.</param>
    /// <param name="sort">What to sort by.</param>
    /// <param name="filter">The text the names must contain, if any.</param>
    /// <param name="direction">Which way to sort.</param>
    /// <param name="pageSize">How many organizations a page holds, if not the default.</param>
    /// <returns>The request.</returns>
    private static OrganizationCollectionRequest CreateRequest(
        RecordStatus status,
        OrganizationSortColumn sort,
        string? filter = null,
        SortDirection direction = SortDirection.Ascending,
        int? pageSize = null)
    {
        int size = pageSize ?? (status == RecordStatus.Active ? 10 : PageSize.Largest);
        return new OrganizationCollectionRequest
        {
            Filter = filter is null ? null : new FilterText(filter),
            Status = status,
            Sort = sort,
            Direction = direction,
            PageSize = new PageSize(size),
        };
    }

    /// <summary>
    /// Creates a delete as an organization's confirm dialog would send it.
    /// </summary>
    /// <param name="id">The organization to delete.</param>
    /// <param name="version">The version the dialog showed it at.</param>
    /// <returns>The delete.</returns>
    private static OrganizationDeleteRequest CreateDelete(OrganizationId id, int version)
    {
        return new OrganizationDeleteRequest
        {
            Id = id,
            Version = new VersionNumber(version),
        };
    }

    /// <summary>
    /// Creates a roll-back as Demo User would send it from the History tab.
    /// </summary>
    /// <param name="id">The organization to roll back.</param>
    /// <param name="version">The version the History tab showed it at.</param>
    /// <param name="to">The version to roll back to.</param>
    /// <returns>The roll-back.</returns>
    private static OrganizationRollBackRequest CreateRollBack(OrganizationId id, int version, int to)
    {
        return new OrganizationRollBackRequest
        {
            Id = id,
            Version = new VersionNumber(version),
            To = new VersionNumber(to),
            ChangedBy = UserSeed.DemoUser,
        };
    }

    /// <summary>
    /// Creates a new organization as Demo User would send it from the create form, in shipping, with no website.
    /// </summary>
    /// <param name="name">The new organization's name.</param>
    /// <returns>The new organization.</returns>
    private static OrganizationCreateRequest CreateNew(string name)
    {
        return new OrganizationCreateRequest
        {
            Name = new OrganizationName(name),
            Industry = Industry.Shipping,
            CreatedBy = UserSeed.DemoUser,
        };
    }

    /// <summary>
    /// Creates a change as Demo User would send it from the edit form. The industry and website default to Placeholder
    /// Logistics' own, so a change that gives its name too changes nothing.
    /// </summary>
    /// <param name="id">The organization to change.</param>
    /// <param name="version">The version the form was filled from.</param>
    /// <param name="name">The name to save.</param>
    /// <param name="industry">The industry to save.</param>
    /// <param name="website">The website to save, if any.</param>
    /// <returns>The change.</returns>
    private static OrganizationUpdateRequest CreateUpdate(
        OrganizationId id,
        int version,
        string name,
        Industry industry = Industry.Shipping,
        string? website = "https://placeholder.example")
    {
        return new OrganizationUpdateRequest
        {
            Id = id,
            Version = new VersionNumber(version),
            Name = new OrganizationName(name),
            Industry = industry,
            Website = website is null ? null : new WebAddress(website),
            ChangedBy = UserSeed.DemoUser,
        };
    }

    #endregion Private Methods
}
