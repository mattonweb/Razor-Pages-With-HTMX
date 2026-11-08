namespace HTMX.Test.Unit.Adapter.Contacts.Data.Seed;

using System;
using System.Linq;
using System.Threading.Tasks;
using HTMX.Adapter.Contacts.Contracts;
using HTMX.Adapter.Contacts.Data;
using HTMX.Adapter.Contacts.Data.Seed;
using HTMX.Adapter.Contacts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="SeedWriter"/> class, and that the seed it writes
/// keeps the application's own rules.
/// </summary>
[TestClass]
public sealed class SeedWriterTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the seed holds 49 users, so the list runs to five pages of ten with a last page of
    /// nine.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_WritesFortyNineUsers()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        (User[] users, _) = await SeedAndReadAsync(clock).ConfigureAwait(false);

        // Assert (Then)
        Assert.HasCount(
            49,
            users,
            "The seed should write 49 users.");
    }

    /// <summary>
    /// Unit test to verify that seven of the users are archived, so the Archived filter has rows to show.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_LeavesSevenUsersArchived()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        (User[] users, _) = await SeedAndReadAsync(clock).ConfigureAwait(false);
        User[] archived = [.. users.Where(static user => user.ArchivedOn is not null)];

        // Assert (Then)
        Assert.HasCount(
            7,
            archived,
            "Seven seed users should be archived, leaving 42 active.");
    }

    /// <summary>
    /// Unit test to verify that Pat Example, the user with the longest history, is user 10007 at version 5, which
    /// the history tests rely on.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_GivesPatExampleFiveVersions()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        (User[] users, _) = await SeedAndReadAsync(clock).ConfigureAwait(false);
        User pat = FindPat(users);

        // Assert (Then)
        Assert.AreEqual(
            10007L,
            pat.Id.Value,
            "Pat Example should be user 10007.");
        Assert.AreEqual(
            5,
            pat.Version.Value,
            "Pat Example should be at version 5.");
    }

    /// <summary>
    /// Unit test to verify that every date is written as a time before the application started: Pat Example was
    /// created 240 days before it, and edited 13 minutes before it.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_StoppedClock_DatesEachChangeBeforeNow()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();
        DateTime created = new DateTime(2026, 3, 12, 9, 0, 0, DateTimeKind.Utc);
        DateTime edited = new DateTime(2026, 11, 7, 8, 47, 0, DateTimeKind.Utc);

        // Act (When)
        (User[] users, _) = await SeedAndReadAsync(clock).ConfigureAwait(false);
        User pat = FindPat(users);

        // Assert (Then)
        Assert.AreEqual(
            created,
            pat.CreatedOn,
            "Pat Example should be created 240 days before the application started.");
        Assert.AreEqual(
            edited,
            pat.LastModifiedOn,
            "Pat Example should be edited 13 minutes before the application started.");
    }

    /// <summary>
    /// Unit test to verify that a demo started a day later sees every date a day later, so the data looks as recent
    /// whenever the demo runs.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_ClockOneDayLater_MovesEveryDateForwardOneDay()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();
        clock.Advance(TimeSpan.FromDays(1));
        DateTime created = new DateTime(2026, 3, 13, 9, 0, 0, DateTimeKind.Utc);
        DateTime archived = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

        // Act (When)
        (User[] users, _) = await SeedAndReadAsync(clock).ConfigureAwait(false);
        User pat = FindPat(users);
        User sam = users.Single(static user => user.Id == UserSeed.SamSample);

        // Assert (Then)
        Assert.AreEqual(
            created,
            pat.CreatedOn,
            "Pat Example's creation should move forward one day.");
        Assert.AreEqual(
            archived,
            sam.ArchivedOn,
            "Sam Sample's archive date should move forward one day.");
    }

    /// <summary>
    /// Unit test to verify that dates read back from SQLite, which stores them as text with no time zone, are marked
    /// as UTC.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_ReadsDatesBackAsUtc()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        (User[] users, _) = await SeedAndReadAsync(clock).ConfigureAwait(false);
        User sam = users.Single(static user => user.Id == UserSeed.SamSample);

        // Assert (Then)
        Assert.AreEqual(
            DateTimeKind.Utc,
            sam.CreatedOn.Kind,
            "A date should be read back as UTC.");
        Assert.AreEqual(
            DateTimeKind.Utc,
            sam.ArchivedOn.GetValueOrDefault().Kind,
            "A date that may be unset should be read back as UTC too.");
    }

    /// <summary>
    /// Unit test to verify that every user's row agrees with their latest version, as a save in the application keeps
    /// them agreeing.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_BuildsEachUserFromTheirLatestVersion()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        (User[] users, UserVersion[] versions) = await SeedAndReadAsync(clock).ConfigureAwait(false);
        User[] disagreeing = [.. users.Where(user => !UserMatchesLatestVersion(user, versions))];

        // Assert (Then)
        Assert.HasCount(
            0,
            disagreeing,
            "No user's row should differ from their latest version.");
    }

    /// <summary>
    /// Unit test to verify that every change in the seed was made by a user who existed and was not archived at the
    /// time, the rule the application will enforce.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_RecordsChangesOnlyByActiveUsers()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        (_, UserVersion[] versions) = await SeedAndReadAsync(clock).ConfigureAwait(false);
        UserVersion[] byInactiveUsers =
        [
            .. versions.Where(version => !WasActive(versions, version.ChangedBy, version.ChangedOn)),
        ];

        // Assert (Then)
        Assert.HasCount(
            0,
            byInactiveUsers,
            "No change should be made by a user who did not exist yet or was archived.");
    }

    /// <summary>
    /// Unit test to verify that the seed holds 1,000 organizations, enough to page through properly at any page size.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_WritesOneThousandOrganizations()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        (Organization[] organizations, _, _) = await SeedAndReadOrganizationsAsync(clock).ConfigureAwait(false);

        // Assert (Then)
        Assert.HasCount(
            1000,
            organizations,
            "The seed should write 1,000 organizations.");
    }

    /// <summary>
    /// Unit test to verify that 51 organizations are archived, so the Archived filter pages too: Mockup Media and Draft
    /// Distribution among them, and every 20th generated organization.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_LeavesFiftyOneOrganizationsArchived()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();
        OrganizationId[] named = [OrganizationSeed.MockupMedia, OrganizationSeed.DraftDistribution];

        // Act (When)
        (Organization[] organizations, _, _) = await SeedAndReadOrganizationsAsync(clock).ConfigureAwait(false);
        OrganizationId[] archived =
        [
            .. organizations
                .Where(static organization => organization.ArchivedOn is not null)
                .Select(static organization => organization.Id),
        ];

        // Assert (Then)
        Assert.HasCount(
            51,
            archived,
            "51 organizations should be archived.");
        CollectionAssert.IsSubsetOf(
            named,
            archived,
            "Mockup Media and Draft Distribution should be among the archived organizations.");
    }

    /// <summary>
    /// Unit test to verify that every organization's row agrees with its latest version.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_BuildsEachOrganizationFromItsLatestVersion()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        (Organization[] organizations, OrganizationVersion[] versions, _) =
            await SeedAndReadOrganizationsAsync(clock).ConfigureAwait(false);
        Organization[] disagreeing =
        [
            .. organizations.Where(organization => !OrganizationMatchesLatestVersion(organization, versions)),
        ];

        // Assert (Then)
        Assert.HasCount(
            0,
            disagreeing,
            "No organization's row should differ from its latest version.");
    }

    /// <summary>
    /// Unit test to verify that every change to an organization was made by a user who existed and was not archived at
    /// the time.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_RecordsOrganizationChangesOnlyByActiveUsers()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        (_, OrganizationVersion[] versions, UserVersion[] userVersions) =
            await SeedAndReadOrganizationsAsync(clock).ConfigureAwait(false);
        OrganizationVersion[] byInactiveUsers =
        [
            .. versions.Where(version => !WasActive(userVersions, version.ChangedBy, version.ChangedOn)),
        ];

        // Assert (Then)
        Assert.HasCount(
            0,
            byInactiveUsers,
            "No organization should be changed by a user who did not exist yet or was archived.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Seeds a new database and reads back every user and every version.
    /// </summary>
    /// <param name="clock">The clock the seed writer reads.</param>
    /// <returns>The users and the versions.</returns>
    private static async Task<(User[] Users, UserVersion[] Versions)> SeedAndReadAsync(TimeProvider clock)
    {
        using (InMemoryDatabase database = await TestDatabase.CreateSeededAsync(clock).ConfigureAwait(false))
        {
            using (ContactsDatabaseContext context = TestDatabase.CreateContext(database))
            {
                User[] users = await context.Set<User>().AsNoTracking().ToArrayAsync().ConfigureAwait(false);
                UserVersion[] versions = await context.Set<UserVersion>().AsNoTracking().ToArrayAsync().ConfigureAwait(false);
                return (users, versions);
            }
        }
    }

    /// <summary>
    /// Seeds a new database and reads back every organization, every organization version, and every user version,
    /// which says when each user was active.
    /// </summary>
    /// <param name="clock">The clock the seed writer reads.</param>
    /// <returns>The organizations, their versions and the users' versions.</returns>
    private static async Task<(Organization[] Organizations, OrganizationVersion[] Versions, UserVersion[] UserVersions)>
        SeedAndReadOrganizationsAsync(TimeProvider clock)
    {
        using (InMemoryDatabase database = await TestDatabase.CreateSeededAsync(clock).ConfigureAwait(false))
        {
            using (ContactsDatabaseContext context = TestDatabase.CreateContext(database))
            {
                Organization[] organizations =
                    await context.Set<Organization>().AsNoTracking().ToArrayAsync().ConfigureAwait(false);
                OrganizationVersion[] versions =
                    await context.Set<OrganizationVersion>().AsNoTracking().ToArrayAsync().ConfigureAwait(false);
                UserVersion[] userVersions =
                    await context.Set<UserVersion>().AsNoTracking().ToArrayAsync().ConfigureAwait(false);
                return (organizations, versions, userVersions);
            }
        }
    }

    /// <summary>
    /// Says whether an organization's row holds the values, state and number of its latest version.
    /// </summary>
    /// <param name="organization">The organization's row.</param>
    /// <param name="versions">Every version of every organization.</param>
    /// <returns><see langword="true"/> when the row and the latest version agree.</returns>
    private static bool OrganizationMatchesLatestVersion(Organization organization, OrganizationVersion[] versions)
    {
        OrganizationVersion latest = versions
            .Where(version => version.OrganizationId == organization.Id)
            .OrderBy(static version => version.Version.Value)
            .Last();
        return latest.Name == organization.Name
            && latest.Industry == organization.Industry
            && latest.Website == organization.Website
            && latest.ArchivedOn == organization.ArchivedOn
            && latest.ArchivedBy == organization.ArchivedBy
            && latest.ChangedOn == organization.LastModifiedOn
            && latest.ChangedBy == organization.LastModifiedBy
            && latest.Version == organization.Version;
    }

    /// <summary>
    /// Finds Pat Example, the user with the longest history.
    /// </summary>
    /// <param name="users">Every seed user.</param>
    /// <returns>Pat Example.</returns>
    private static User FindPat(User[] users)
    {
        return users.Single(static user => user.Id == UserSeed.PatExample);
    }

    /// <summary>
    /// Says whether a user's row holds the values, state and number of their latest version.
    /// </summary>
    /// <param name="user">The user's row.</param>
    /// <param name="versions">Every version of every user.</param>
    /// <returns><see langword="true"/> when the row and the latest version agree.</returns>
    private static bool UserMatchesLatestVersion(User user, UserVersion[] versions)
    {
        UserVersion latest = versions
            .Where(version => version.UserId == user.Id)
            .OrderBy(static version => version.Version.Value)
            .Last();
        return latest.Username == user.Username
            && latest.Email == user.Email
            && latest.DisplayName == user.DisplayName
            && latest.ArchivedOn == user.ArchivedOn
            && latest.ArchivedBy == user.ArchivedBy
            && latest.ChangedOn == user.LastModifiedOn
            && latest.ChangedBy == user.LastModifiedBy
            && latest.Version == user.Version;
    }

    /// <summary>
    /// Says whether a user existed and was active at a moment, from their own versions up to that moment.
    /// </summary>
    /// <param name="versions">Every version of every user.</param>
    /// <param name="user">The user to ask about.</param>
    /// <param name="moment">The moment to ask about.</param>
    /// <returns><see langword="true"/> when the user had been created and was not archived.</returns>
    private static bool WasActive(UserVersion[] versions, UserId user, DateTime moment)
    {
        UserVersion? stateThen = versions
            .Where(version => version.UserId == user && version.ChangedOn <= moment)
            .OrderBy(static version => version.Version.Value)
            .LastOrDefault();
        return stateThen is not null && stateThen.ArchivedOn is null;
    }

    #endregion Private Methods
}
