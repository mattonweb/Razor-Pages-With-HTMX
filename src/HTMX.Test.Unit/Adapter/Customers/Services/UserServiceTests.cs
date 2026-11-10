namespace HTMX.Test.Unit.Adapter.Customers.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data.Seed;
using HTMX.Adapter.Customers.Entities;
using HTMX.Test.Unit.Adapter.Customers.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the user service behind <see cref="IUserService"/>, over a seeded
/// in-memory database: the pages of the users list, filtered, narrowed by status, sorted by each column either way,
/// and paged by key; one user and their versions; and changing, archiving, restoring and rolling back a user.
/// </summary>
[TestClass]
public sealed class UserServiceTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the list's first view, active users by name, holds a page of ten in name order, with
    /// the counts the status filter shows, and a next page but no previous.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_FirstPageOfActive_ReturnsTenInNameOrder()
    {
        // Arrange (Given)
        UserCollectionRequest request = CreateRequest(RecordStatus.Active, UserSortColumn.Name);

        // Act (When)
        UserCollectionResponse response = await UserServiceCalls.ListAsync(request).ConfigureAwait(false);
        string[] names = [.. response.Items.Select(static item => item.DisplayName.Value)];
        string[] sorted = [.. names.Order(StringComparer.OrdinalIgnoreCase)];
        UserCursor next = new UserCursor(response.Items[^1].Id, CursorPosition.After);

        // Assert (Then)
        CollectionAssert.AreEqual(
            sorted,
            names,
            "The page should be in name order, ignoring capitals.");
        Assert.AreEqual(
            (10, 42, 7, 49),
            (response.Items.Length, response.Matching.Value, response.Archived.Value, response.All.Value),
            "The page should hold ten of the 42 active users, with 7 archived and 49 in all.");
        Assert.AreEqual(
            ((UserCursor?)null, (UserCursor?)next),
            (response.Previous, response.Next),
            "The first page should have no previous page, and its next should start after its last user.");
    }

    /// <summary>
    /// Unit test to verify that the filter finds its text in a name, a username or an email address, however either is
    /// capitalized.
    /// </summary>
    /// <param name="filter">The text to find.</param>
    /// <param name="expected">The username of a user it should find.</param>
    [TestMethod]
    [DataRow("y d", "avery.draft", DisplayName = "In a name: Avery Draft")]
    [DataRow("Y.D", "avery.draft", DisplayName = "In a username: avery.draft")]
    [DataRow("EXAMPLE.NET", "jordan.demo", DisplayName = "In an email address: jordan@example.net")]
    public async Task ListAsync_Filter_FindsItInTheNameUsernameOrEmail(string filter, string expected)
    {
        // Arrange (Given)
        UserCollectionRequest request = CreateRequest(RecordStatus.All, UserSortColumn.Name, filter: filter);

        // Act (When)
        UserCollectionResponse response = await UserServiceCalls.ListAsync(request).ConfigureAwait(false);
        string[] usernames = [.. response.Items.Select(static item => item.Username.Value)];

        // Assert (Then)
        CollectionAssert.Contains(
            usernames,
            expected,
            $"The filter {filter} should find {expected}.");
        Assert.IsTrue(
            response.Items.All(item => Holds(item, filter)),
            "Every user found should hold the filter in their name, username or email address.");
    }

    /// <summary>
    /// Unit test to verify that Previous from the second page leads back to the first page, whole.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_PreviousFromTheSecondPage_ReturnsTheFirstPage()
    {
        // Arrange (Given)
        UserCollectionRequest request = CreateRequest(RecordStatus.Active, UserSortColumn.Name);

        // Act (When)
        UserCollectionResponse[] pages = await UserServiceCalls
            .ReadInTurnAsync(request, CursorPosition.After, CursorPosition.Before)
            .ConfigureAwait(false);

        // Assert (Then)
        CollectionAssert.AreEqual(
            pages[0].Items.Select(static item => item.Id).ToArray(),
            pages[2].Items.Select(static item => item.Id).ToArray(),
            "Next and then Previous should show the first page again.");
        Assert.IsNull(
            pages[2].Previous,
            "Back on the first page, there should be no previous page.");
    }

    /// <summary>
    /// Unit test to verify that a cursor naming no user, as an old link might, shows the first page.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_CursorNamesNoUser_ReturnsTheFirstPage()
    {
        // Arrange (Given)
        UserCollectionRequest first = CreateRequest(RecordStatus.Active, UserSortColumn.Name);
        UserCursor nobody = new UserCursor(new UserId(99999), CursorPosition.After);

        // Act (When)
        UserCollectionResponse response = await UserServiceCalls
            .ListAsync(UserServiceCalls.At(first, nobody))
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ((UserCursor?)null, (UserCursor?)null, 10),
            (response.Request.Cursor, response.Previous, response.Items.Length),
            "The answer should be the first page, which forgets the cursor.");
    }

    /// <summary>
    /// Unit test to verify that following Next from the first page to the last shows every user once, in the order the
    /// list is sorted, by each column and either way: each sort's seek leaves no one out and shows no one twice.
    /// </summary>
    /// <param name="sort">What to sort by.</param>
    /// <param name="direction">Which way to sort.</param>
    [TestMethod]
    [DataRow(UserSortColumn.Name, SortDirection.Ascending)]
    [DataRow(UserSortColumn.Name, SortDirection.Descending)]
    [DataRow(UserSortColumn.Username, SortDirection.Ascending)]
    [DataRow(UserSortColumn.Username, SortDirection.Descending)]
    [DataRow(UserSortColumn.Email, SortDirection.Ascending)]
    [DataRow(UserSortColumn.Email, SortDirection.Descending)]
    [DataRow(UserSortColumn.Updated, SortDirection.Ascending)]
    [DataRow(UserSortColumn.Updated, SortDirection.Descending)]
    public async Task ListAsync_FollowingNextToTheEnd_VisitsEveryUserOnceInOrder(
        UserSortColumn sort,
        SortDirection direction)
    {
        // Arrange (Given)
        UserCollectionRequest request = CreateRequest(RecordStatus.All, sort, direction: direction);

        // Act (When)
        UserCollectionResponse[] pages = await UserServiceCalls.WalkToTheEndAsync(request).ConfigureAwait(false);
        UserSummary[] visited = [.. pages.SelectMany(static page => page.Items)];
        UserId[] expected = [.. Sorted(visited, sort, direction).Select(static item => item.Id)];

        // Assert (Then)
        Assert.AreEqual(
            (5, 9, (UserCursor?)null),
            (pages.Length, pages[^1].Items.Length, pages[^1].Next),
            "49 users, ten to a page, should take five pages, the last of nine, with no next page.");
        CollectionAssert.AreEqual(
            expected,
            visited.Select(static item => item.Id).ToArray(),
            "Every user should be shown once, in the list's order, ties in identifier order.");
    }

    /// <summary>
    /// Unit test to verify that one user is read with every value they hold, and who created and last changed them.
    /// </summary>
    [TestMethod]
    public async Task FindAsync_PatExample_ReturnsTheirValuesAndWhoChangedThem()
    {
        // Arrange (Given)
        UserId id = UserSeed.PatExample;

        // Act (When)
        UserSingleResponse? found = await UserServiceCalls.FindAsync(id).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNotNull(
            found,
            "Pat Example should be found.");
        Assert.AreEqual(
            ("Pat Example", "pat.example", "pat@example.com", new VersionNumber(5)),
            (found.DisplayName.Value, found.Username.Value, found.Email.Value, found.Version),
            "Pat should hold the values of their fifth version.");
        Assert.AreEqual(
            ("Demo User", "Demo User", TestDatabase.StartedOn.AddMinutes(-13)),
            (found.Created.By.Name.Value, found.LastModified.By.Name.Value, found.LastModified.On),
            "Pat should say who created them, and who last changed them and when.");
        Assert.IsNull(
            found.Archived,
            "Pat was restored, so they should not say they are archived.");
    }

    /// <summary>
    /// Unit test to verify that an archived user says who archived them and when.
    /// </summary>
    [TestMethod]
    public async Task FindAsync_SamSample_SaysWhoArchivedThem()
    {
        // Arrange (Given)
        UserId id = UserSeed.SamSample;

        // Act (When)
        UserSingleResponse? found = await UserServiceCalls.FindAsync(id).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (UserSeed.DemoUser, TestDatabase.StartedOn.AddDays(-38)),
            (found?.Archived?.By.Id, found?.Archived?.On),
            "Sam should say Demo User archived them 38 days ago.");
    }

    /// <summary>
    /// Unit test to verify that an identifier that names no user finds nothing.
    /// </summary>
    [TestMethod]
    public async Task FindAsync_UnknownIdentifier_ReturnsNull()
    {
        // Arrange (Given)
        UserId id = new UserId(99999);

        // Act (When)
        UserSingleResponse? found = await UserServiceCalls.FindAsync(id).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNull(
            found,
            "No user has the identifier 99999.");
    }

    /// <summary>
    /// Unit test to verify that a user's versions come newest first, each with what happened, who did it and the
    /// values it left.
    /// </summary>
    [TestMethod]
    public async Task ListVersionsAsync_PatExample_ReturnsNewestFirstWithWhoMadeEach()
    {
        // Arrange (Given)
        UserId id = UserSeed.PatExample;

        // Act (When)
        UserVersionResponse[] versions = await UserServiceCalls.ListVersionsAsync(id).ConfigureAwait(false);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { 5, 4, 3, 2, 1 },
            versions.Select(static version => version.Version.Value).ToArray(),
            "Pat's five versions should come newest first.");
        Assert.AreEqual(
            (ChangeKind.Archived, "Sam Sample", "Pat Exampel"),
            (versions[3].Change, versions[3].Changed.By.Name.Value, versions[3].DisplayName.Value),
            "Version 2 should say Sam archived Pat, under the name Pat had then.");
        Assert.IsNotNull(
            versions[3].Changed.By.ArchivedOn,
            "Sam has since been archived, and the version should say so.");
    }

    /// <summary>
    /// Unit test to verify that a change from the current version is saved as the next one: the user takes its values,
    /// says who changed them and when, and their history gains an Edited version holding the same values.
    /// </summary>
    [TestMethod]
    public async Task UpdateAsync_NewValuesFromTheCurrentVersion_SavesTheNextVersion()
    {
        // Arrange (Given)
        UserId id = UserSeed.PatExample;
        UserUpdateRequest request = CreateUpdate(id, 5, "patricia.example", "patricia@example.com", "Patricia Example");

        // Act (When)
        (UpdateOutcome[] outcomes, UserSingleResponse? after, UserVersion[] history) =
            await UserServiceCalls.UpdateInTurnAsync(id, request).ConfigureAwait(false);
        UserVersion latest = history[^1];

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.Saved,
            outcomes[0],
            "A change from the current version should be saved.");
        Assert.AreEqual(
            ("Patricia Example", "patricia.example", "patricia@example.com", new VersionNumber(6)),
            (after?.DisplayName.Value, after?.Username.Value, after?.Email.Value, after?.Version),
            "Pat should take the new values as version 6.");
        Assert.AreEqual(
            (UserSeed.DemoUser, TestDatabase.StartedOn),
            (after?.LastModified.By.Id, after?.LastModified.On),
            "The change should name who saved it, dated by the clock.");
        Assert.AreEqual(
            (ChangeKind.Edited, "patricia.example"),
            (latest.Change, latest.Username.Value),
            "The new version should record an edit, holding the values Pat now has.");
    }

    /// <summary>
    /// Unit test to verify that a change the rules refuse, or that changes nothing, saves nothing: the user keeps their
    /// version and their history gains no version. The rules run in order, so each row breaks only one.
    /// </summary>
    /// <param name="id">The user the change is for.</param>
    /// <param name="version">The version the change starts from.</param>
    /// <param name="username">The username the change gives.</param>
    /// <param name="email">The email address the change gives.</param>
    /// <param name="expected">What should become of the change.</param>
    [TestMethod]
    [DataRow(10007L, 5, "pat.example", "pat@example.com", UpdateOutcome.Unchanged, DisplayName = "The values they hold")]
    [DataRow(10007L, 4, "pat.example", "pat@example.net", UpdateOutcome.Overtaken, DisplayName = "From an older version")]
    [DataRow(10007L, 5, "SAM.SAMPLE", "pat@example.com", UpdateOutcome.NameTaken, DisplayName = "Another's username")]
    [DataRow(10007L, 5, "pat.example", "TAYLOR@EXAMPLE.COM", UpdateOutcome.EmailTaken, DisplayName = "Another's email")]
    [DataRow(10002L, 2, "casey.mockup", "casey@example.net", UpdateOutcome.Archived, DisplayName = "An archived user")]
    [DataRow(99999L, 1, "nobody", "nobody@example.com", UpdateOutcome.NotFound, DisplayName = "An unknown identifier")]
    public async Task UpdateAsync_RefusedOrUnchanged_SavesNothing(
        long id,
        int version,
        string username,
        string email,
        UpdateOutcome expected)
    {
        // Arrange (Given)
        UserId userId = new UserId(id);
        UserUpdateRequest request = CreateUpdate(userId, version, username, email);
        UserSingleResponse? before = await UserServiceCalls.FindAsync(userId).ConfigureAwait(false);

        // Act (When)
        (UpdateOutcome[] outcomes, UserSingleResponse? after, UserVersion[] history) =
            await UserServiceCalls.UpdateInTurnAsync(userId, request).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            outcomes[0],
            $"The change should be answered {expected}.");
        Assert.AreEqual(
            before?.Version,
            after?.Version,
            "The user should keep their version.");
        Assert.HasCount(
            before is null ? 0 : before.Version.Value,
            history,
            "The history should gain no version.");
    }

    /// <summary>
    /// Unit test to verify that a user can take their own username and email address in other capitals: a value
    /// counts as taken only when another user has it.
    /// </summary>
    [TestMethod]
    public async Task UpdateAsync_OwnValuesInCapitals_SavesThem()
    {
        // Arrange (Given)
        UserId id = UserSeed.PatExample;
        UserUpdateRequest request = CreateUpdate(id, 5, "PAT.EXAMPLE", "PAT@EXAMPLE.COM");

        // Act (When)
        (UpdateOutcome[] outcomes, UserSingleResponse? after, _) =
            await UserServiceCalls.UpdateInTurnAsync(id, request).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.Saved,
            outcomes[0],
            "Pat's own values in capitals should not count as taken.");
        Assert.AreEqual(
            ("PAT.EXAMPLE", "PAT@EXAMPLE.COM"),
            (after?.Username.Value, after?.Email.Value),
            "Pat should take the new capitals.");
    }

    /// <summary>
    /// Unit test to verify that of two changes started from the same version, as from two browser tabs, the first is
    /// saved and the second is answered Overtaken rather than saved over it.
    /// </summary>
    [TestMethod]
    public async Task UpdateAsync_SecondChangeFromTheSameVersion_IsOvertaken()
    {
        // Arrange (Given)
        UserId id = UserSeed.PatExample;
        UserUpdateRequest first = CreateUpdate(id, 5, "pat.example", "pat@example.net");
        UserUpdateRequest second = CreateUpdate(id, 5, "pat.example", "pat@example.org");

        // Act (When)
        (UpdateOutcome[] outcomes, UserSingleResponse? after, UserVersion[] history) =
            await UserServiceCalls.UpdateInTurnAsync(id, first, second).ConfigureAwait(false);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { UpdateOutcome.Saved, UpdateOutcome.Overtaken },
            outcomes,
            "The first change should be saved and the second refused.");
        Assert.AreEqual(
            "pat@example.net",
            after?.Email.Value,
            "Pat should keep the first change.");
        Assert.HasCount(
            6,
            history,
            "Only the first change should add a version.");
    }

    /// <summary>
    /// Unit test to verify that archiving an active user saves an Archived version that says who archived them and
    /// when, keeps their values, and leaves them archived at that version.
    /// </summary>
    [TestMethod]
    public async Task ArchiveAsync_ActiveUser_ArchivesThemAsTheNextVersion()
    {
        // Arrange (Given)
        UserStatusRequest request = CreateStatusRequest(UserSeed.PatExample);

        // Act (When)
        (UpdateOutcome outcome, UserSingleResponse? after, UserVersion[] history) =
            await UserServiceCalls.ChangeStatusAsync(request, ChangeKind.Archived).ConfigureAwait(false);
        UserVersion latest = history[^1];

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.Saved,
            outcome,
            "An active user should be archived.");
        Assert.AreEqual(
            (UserSeed.DemoUser, TestDatabase.StartedOn),
            (after?.Archived?.By.Id, after?.Archived?.On),
            "Pat should say Demo User archived them, now.");
        Assert.AreEqual(
            (new VersionNumber(6), "pat@example.com"),
            (after?.Version, after?.Email.Value),
            "Archiving should move Pat to version 6 and keep their values.");
        Assert.AreEqual(
            (ChangeKind.Archived, (UserId?)UserSeed.DemoUser),
            (latest.Change, latest.ArchivedBy),
            "The new version should record the archiving, and who did it.");
    }

    /// <summary>
    /// Unit test to verify that restoring an archived user saves a Restored version and leaves them active at that
    /// version.
    /// </summary>
    [TestMethod]
    public async Task RestoreAsync_ArchivedUser_RestoresThemAsTheNextVersion()
    {
        // Arrange (Given)
        UserStatusRequest request = CreateStatusRequest(new UserId(10002));

        // Act (When)
        (UpdateOutcome outcome, UserSingleResponse? after, UserVersion[] history) =
            await UserServiceCalls.ChangeStatusAsync(request, ChangeKind.Restored).ConfigureAwait(false);
        UserVersion latest = history[^1];

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.Saved,
            outcome,
            "An archived user should be restored.");
        Assert.IsNotNull(
            after,
            "Casey Mockup should still be found.");
        Assert.IsNull(
            after.Archived,
            "Casey should be active again.");
        Assert.AreEqual(
            (new VersionNumber(3), ChangeKind.Restored, (DateTime?)null),
            (after.Version, latest.Change, latest.ArchivedOn),
            "Restoring should move Casey from version 2 to version 3, a Restored version that leaves them active.");
    }

    /// <summary>
    /// Unit test to verify that archiving the signed-in user themselves, archiving a user already archived, restoring
    /// one already active, or either for an unknown identifier, saves nothing.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="change">Archived to archive them, Restored to restore them.</param>
    /// <param name="expected">What should become of the request.</param>
    [TestMethod]
    [DataRow(10001L, ChangeKind.Archived, UpdateOutcome.SelfArchive, DisplayName = "Demo User archiving themselves")]
    [DataRow(10002L, ChangeKind.Archived, UpdateOutcome.Unchanged, DisplayName = "Archive one already archived")]
    [DataRow(10007L, ChangeKind.Restored, UpdateOutcome.Unchanged, DisplayName = "Restore one already active")]
    [DataRow(99999L, ChangeKind.Archived, UpdateOutcome.NotFound, DisplayName = "An unknown identifier")]
    public async Task ChangeStatus_RefusedOrAlreadyThatWay_SavesNothing(
        long id,
        ChangeKind change,
        UpdateOutcome expected)
    {
        // Arrange (Given)
        UserId userId = new UserId(id);
        UserSingleResponse? before = await UserServiceCalls.FindAsync(userId).ConfigureAwait(false);

        // Act (When)
        (UpdateOutcome outcome, UserSingleResponse? after, UserVersion[] history) =
            await UserServiceCalls.ChangeStatusAsync(CreateStatusRequest(userId), change).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            outcome,
            $"The request should be answered {expected}.");
        Assert.AreEqual(
            before?.Version,
            after?.Version,
            "The user should keep their version.");
        Assert.HasCount(
            before is null ? 0 : before.Version.Value,
            history,
            "The history should gain no version.");
    }

    /// <summary>
    /// Unit test to verify that rolling back saves the earlier version's values as the next version, a RolledBack one
    /// that names the version it went back to.
    /// </summary>
    [TestMethod]
    public async Task RollBackAsync_ToAnEarlierVersion_SavesItsValuesAsTheNextVersion()
    {
        // Arrange (Given)
        UserRollBackRequest request = CreateRollBack(UserSeed.PatExample, 5, 1);

        // Act (When)
        (UpdateOutcome outcome, UserSingleResponse? after, UserVersion[] history) =
            await UserServiceCalls.RollBackAsync(request).ConfigureAwait(false);
        UserVersion latest = history[^1];

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.Saved,
            outcome,
            "Rolling back to a version with other values should be saved.");
        Assert.AreEqual(
            ("Pat Exampel", "pat@placeholder.example"),
            (after?.DisplayName.Value, after?.Email.Value),
            "Pat should take version 1's name and email address again.");
        Assert.AreEqual(
            new VersionNumber(6),
            after?.Version,
            "The roll-back should be version 6, not a return to version 1.");
        Assert.AreEqual(
            (ChangeKind.RolledBack, (VersionNumber?)new VersionNumber(1)),
            (latest.Change, latest.RolledBackTo),
            "The new version should record a roll-back to version 1.");
    }

    /// <summary>
    /// Unit test to verify that a roll-back the rules refuse, or that changes nothing, saves nothing.
    /// </summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="version">The version the request says the user is at.</param>
    /// <param name="to">The version to roll back to.</param>
    /// <param name="expected">What should become of the request.</param>
    [TestMethod]
    [DataRow(10007L, 4, 1, UpdateOutcome.Overtaken, DisplayName = "From a version they have moved on from")]
    [DataRow(10007L, 5, 5, UpdateOutcome.Unchanged, DisplayName = "To the version they are at")]
    [DataRow(10002L, 2, 1, UpdateOutcome.Archived, DisplayName = "An archived user")]
    [DataRow(10007L, 5, 9, UpdateOutcome.NotFound, DisplayName = "To a version they never had")]
    [DataRow(99999L, 1, 1, UpdateOutcome.NotFound, DisplayName = "An unknown identifier")]
    public async Task RollBackAsync_RefusedOrUnchanged_SavesNothing(
        long id,
        int version,
        int to,
        UpdateOutcome expected)
    {
        // Arrange (Given)
        UserId userId = new UserId(id);
        UserSingleResponse? before = await UserServiceCalls.FindAsync(userId).ConfigureAwait(false);

        // Act (When)
        (UpdateOutcome outcome, UserSingleResponse? after, _) =
            await UserServiceCalls.RollBackAsync(CreateRollBack(userId, version, to)).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            outcome,
            $"The roll-back should be answered {expected}.");
        Assert.AreEqual(
            before?.Version,
            after?.Version,
            "The user should keep their version.");
    }

    /// <summary>
    /// Unit test to verify that a roll-back to an email address another user has taken since, in other capitals, is
    /// refused, and saves nothing.
    /// </summary>
    [TestMethod]
    public async Task RollBackAsync_ToAnEmailAnotherUserNowHas_SavesNothing()
    {
        // Arrange (Given)
        UserRollBackRequest request = CreateRollBack(UserSeed.TaylorDummy, 2, 1);
        (UserId, string?, string?) robinTakesTaylorsOldEmail = (new UserId(10010), null, "TAYLOR@DUMMY.EXAMPLE");

        // Act (When)
        (UpdateOutcome outcome, UserSingleResponse? after, _) =
            await UserServiceCalls.RollBackAsync(request, robinTakesTaylorsOldEmail).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.EmailTaken,
            outcome,
            "Robin now has the email address Taylor had at version 1.");
        Assert.AreEqual(
            (new VersionNumber(2), "taylor@example.com"),
            (after?.Version, after?.Email.Value),
            "Taylor should keep their version and their email address.");
    }

    /// <summary>
    /// Unit test to verify that a roll-back to a username another user has taken since, in other capitals, is refused,
    /// and saves nothing.
    /// </summary>
    [TestMethod]
    public async Task RollBackAsync_ToAUsernameAnotherUserNowHas_SavesNothing()
    {
        // Arrange (Given)
        UserRollBackRequest request = CreateRollBack(UserSeed.PatExample, 5, 4);
        (UserId, string?, string?) patIsRenamed = (UserSeed.PatExample, "pat.renamed", null);
        (UserId, string?, string?) robinTakesPatsOldUsername = (new UserId(10010), "PAT.EXAMPLE", null);

        // Act (When)
        (UpdateOutcome outcome, UserSingleResponse? after, _) = await UserServiceCalls
            .RollBackAsync(request, patIsRenamed, robinTakesPatsOldUsername)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            UpdateOutcome.NameTaken,
            outcome,
            "Robin now has the username Pat had at version 4.");
        Assert.AreEqual(
            (new VersionNumber(5), "pat.renamed"),
            (after?.Version, after?.Username.Value),
            "Pat should keep their version and their username.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the request for the first page of the users list.
    /// </summary>
    /// <param name="status">Which users to show.</param>
    /// <param name="sort">What to sort by.</param>
    /// <param name="filter">The text to find, if any.</param>
    /// <param name="direction">Which way to sort.</param>
    /// <returns>The request, ten to a page.</returns>
    private static UserCollectionRequest CreateRequest(
        RecordStatus status,
        UserSortColumn sort,
        string? filter = null,
        SortDirection direction = SortDirection.Ascending)
    {
        return new UserCollectionRequest
        {
            Filter = filter is null ? null : new FilterText(filter),
            Status = status,
            Sort = sort,
            Direction = direction,
            PageSize = new PageSize(10),
        };
    }

    /// <summary>
    /// Creates an archive or a restore as Demo User would send it from a user's page.
    /// </summary>
    /// <param name="id">The user to archive or restore.</param>
    /// <returns>The request.</returns>
    private static UserStatusRequest CreateStatusRequest(UserId id)
    {
        return new UserStatusRequest
        {
            Id = id,
            ChangedBy = UserSeed.DemoUser,
        };
    }

    /// <summary>
    /// Creates a change as Demo User would send it from the edit form.
    /// </summary>
    /// <param name="id">The user to change.</param>
    /// <param name="version">The version the form was filled from.</param>
    /// <param name="username">The username to save.</param>
    /// <param name="email">The email address to save.</param>
    /// <param name="name">The name to save; Pat's own unless given.</param>
    /// <returns>The change.</returns>
    private static UserUpdateRequest CreateUpdate(
        UserId id,
        int version,
        string username,
        string email,
        string name = "Pat Example")
    {
        return new UserUpdateRequest
        {
            Id = id,
            Version = new VersionNumber(version),
            DisplayName = new DisplayName(name),
            Username = new Username(username),
            Email = new EmailAddress(email),
            ChangedBy = UserSeed.DemoUser,
        };
    }

    /// <summary>
    /// Creates a roll-back as Demo User would send it from the History tab.
    /// </summary>
    /// <param name="id">The user to roll back.</param>
    /// <param name="version">The version the History tab showed them at.</param>
    /// <param name="to">The version to roll back to.</param>
    /// <returns>The roll-back.</returns>
    private static UserRollBackRequest CreateRollBack(UserId id, int version, int to)
    {
        return new UserRollBackRequest
        {
            Id = id,
            Version = new VersionNumber(version),
            To = new VersionNumber(to),
            ChangedBy = UserSeed.DemoUser,
        };
    }

    /// <summary>
    /// Says whether a user holds the filter's text in their name, username or email address, ignoring capitals.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="filter">The text.</param>
    /// <returns><see langword="true"/> when one of the three holds it.</returns>
    private static bool Holds(UserSummary user, string filter)
    {
        string[] values = [user.DisplayName.Value, user.Username.Value, user.Email.Value];
        return values.Any(value => value.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Sorts users as the list should: by the column's text ignoring capitals, or by when they were last changed, then
    /// by identifier, all one way.
    /// </summary>
    /// <param name="users">The users.</param>
    /// <param name="sort">What to sort by.</param>
    /// <param name="direction">Which way to sort.</param>
    /// <returns>The users in the order expected.</returns>
    private static UserSummary[] Sorted(UserSummary[] users, UserSortColumn sort, SortDirection direction)
    {
        // A round-trip date sorts as text in time order, so every column can be compared as text.
        Func<UserSummary, string> key = sort switch
        {
            UserSortColumn.Username =>
                static user => user.Username.Value,
            UserSortColumn.Email =>
                static user => user.Email.Value,
            UserSortColumn.Updated =>
                static user => user.LastModifiedOn.ToString("O", CultureInfo.InvariantCulture),
            _ =>
                static user => user.DisplayName.Value,
        };
        IOrderedEnumerable<UserSummary> ordered = direction == SortDirection.Descending
            ? users.OrderByDescending(key, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(static user => user.Id.Value)
            : users.OrderBy(key, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static user => user.Id.Value);
        return [.. ordered];
    }

    #endregion Private Methods
}
