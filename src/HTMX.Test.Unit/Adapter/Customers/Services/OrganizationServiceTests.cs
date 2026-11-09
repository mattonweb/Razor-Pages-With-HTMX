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
