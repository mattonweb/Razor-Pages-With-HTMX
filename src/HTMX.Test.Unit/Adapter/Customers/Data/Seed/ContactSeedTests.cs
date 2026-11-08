namespace HTMX.Test.Unit.Adapter.Customers.Data.Seed;

using System;
using System.Linq;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data;
using HTMX.Adapter.Customers.Data.Seed;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the contacts <see cref="ContactSeed"/> and <see cref="GeneratedContactSeed"/>
/// put in the database, through <see cref="SeedWriter"/>: how many, and that they keep the application's own rules.
/// </summary>
[TestClass]
public sealed class ContactSeedTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the seed holds 1,500 contacts, enough to page through at any page size.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_WritesFifteenHundredContacts()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        SeededContacts seeded = await SeedAndReadAsync(clock).ConfigureAwait(false);

        // Assert (Then)
        Assert.HasCount(
            1500,
            seeded.Contacts,
            "The seed should write 1,500 contacts.");
    }

    /// <summary>
    /// Unit test to verify that 60 contacts are archived: Darcy Dataset and every 25th generated contact.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_LeavesSixtyContactsArchived()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        SeededContacts seeded = await SeedAndReadAsync(clock).ConfigureAwait(false);
        Contact[] archived = [.. seeded.Contacts.Where(static contact => contact.ArchivedOn is not null)];

        // Assert (Then)
        Assert.HasCount(
            60,
            archived,
            "60 contacts should be archived.");
    }

    /// <summary>
    /// Unit test to verify that Placeholder Logistics has enough contacts for its own list of them to page, and that
    /// Draft Distribution, which is meant to be deletable, has none.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_GivesPlaceholderLogisticsEighteenContacts()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        SeededContacts seeded = await SeedAndReadAsync(clock).ConfigureAwait(false);
        Contact[] atPlaceholder =
        [
            .. seeded.Contacts.Where(static contact => contact.OrganizationId == OrganizationSeed.PlaceholderLogistics),
        ];
        Contact[] atDraft =
        [
            .. seeded.Contacts.Where(static contact => contact.OrganizationId == OrganizationSeed.DraftDistribution),
        ];

        // Assert (Then)
        Assert.HasCount(
            18,
            atPlaceholder,
            "Placeholder Logistics should have 18 contacts.");
        Assert.HasCount(
            0,
            atDraft,
            "Draft Distribution should have no contacts.");
    }

    /// <summary>
    /// Unit test to verify that every contact's row agrees with their latest version.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_BuildsEachContactFromTheirLatestVersion()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        SeededContacts seeded = await SeedAndReadAsync(clock).ConfigureAwait(false);
        Contact[] disagreeing = [.. seeded.Contacts.Where(contact => !MatchesLatestVersion(contact, seeded.Versions))];

        // Assert (Then)
        Assert.HasCount(
            0,
            disagreeing,
            "No contact's row should differ from their latest version.");
    }

    /// <summary>
    /// Unit test to verify that every change to a contact was made by a user who existed and was not archived at the
    /// time.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_RecordsContactChangesOnlyByActiveUsers()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        SeededContacts seeded = await SeedAndReadAsync(clock).ConfigureAwait(false);
        ContactVersion[] byInactiveUsers =
        [
            .. seeded.Versions.Where(version =>
                !UserWasActive(seeded.UserVersions, version.ChangedBy, version.ChangedOn)),
        ];

        // Assert (Then)
        Assert.HasCount(
            0,
            byInactiveUsers,
            "No contact should be changed by a user who did not exist yet or was archived.");
    }

    /// <summary>
    /// Unit test to verify that a contact joins an organization, when created or when moved, only while that
    /// organization exists and is not archived, as the organization picker will allow.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_NewDatabase_JoinsContactsOnlyToActiveOrganizations()
    {
        // Arrange (Given)
        FakeTimeProvider clock = TestDatabase.CreateClock();

        // Act (When)
        SeededContacts seeded = await SeedAndReadAsync(clock).ConfigureAwait(false);
        ContactVersion[] joinings = [.. seeded.Versions.Where(version => IsJoining(version, seeded.Versions))];
        ContactVersion[] intoInactive =
        [
            .. joinings.Where(version =>
                !OrganizationWasActive(seeded.OrganizationVersions, version.OrganizationId, version.ChangedOn)),
        ];

        // Assert (Then)
        Assert.HasCount(
            1501,
            joinings,
            "Each of the 1,500 contacts should join once, and Ainsley Archetype once more on moving.");
        Assert.HasCount(
            0,
            intoInactive,
            "No contact should join an organization that did not exist yet or was archived.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Seeds a new database and reads back every contact and the versions the rules are checked against.
    /// </summary>
    /// <param name="clock">The clock the seed writer reads.</param>
    /// <returns>The contacts and the versions.</returns>
    private static async Task<SeededContacts> SeedAndReadAsync(TimeProvider clock)
    {
        using (InMemoryDatabase database = await TestDatabase.CreateSeededAsync(clock).ConfigureAwait(false))
        {
            using (CustomersDatabaseContext context = TestDatabase.CreateContext(database))
            {
                return new SeededContacts
                {
                    Contacts = await context.Set<Contact>().AsNoTracking().ToArrayAsync().ConfigureAwait(false),
                    Versions = await context.Set<ContactVersion>().AsNoTracking().ToArrayAsync().ConfigureAwait(false),
                    OrganizationVersions =
                        await context.Set<OrganizationVersion>().AsNoTracking().ToArrayAsync().ConfigureAwait(false),
                    UserVersions = await context.Set<UserVersion>().AsNoTracking().ToArrayAsync().ConfigureAwait(false),
                };
            }
        }
    }

    /// <summary>
    /// Says whether a contact's row holds the values, state and number of their latest version.
    /// </summary>
    /// <param name="contact">The contact's row.</param>
    /// <param name="versions">Every version of every contact.</param>
    /// <returns><see langword="true"/> when the row and the latest version agree.</returns>
    private static bool MatchesLatestVersion(Contact contact, ContactVersion[] versions)
    {
        ContactVersion latest = versions
            .Where(version => version.ContactId == contact.Id)
            .OrderBy(static version => version.Version.Value)
            .Last();
        return latest.Name == contact.Name
            && latest.Email == contact.Email
            && latest.Phone == contact.Phone
            && latest.JobTitle == contact.JobTitle
            && latest.OrganizationId == contact.OrganizationId
            && latest.Notes == contact.Notes
            && latest.ArchivedOn == contact.ArchivedOn
            && latest.ArchivedBy == contact.ArchivedBy
            && latest.ChangedOn == contact.LastModifiedOn
            && latest.ChangedBy == contact.LastModifiedBy
            && latest.Version == contact.Version;
    }

    /// <summary>
    /// Says whether a version puts its contact in an organization: the first version does, and so does any version
    /// whose organization differs from the version before it.
    /// </summary>
    /// <param name="version">The version to ask about.</param>
    /// <param name="versions">Every version of every contact.</param>
    /// <returns><see langword="true"/> when the version joins the contact to an organization.</returns>
    private static bool IsJoining(ContactVersion version, ContactVersion[] versions)
    {
        ContactVersion? previous = versions.SingleOrDefault(other =>
            other.ContactId == version.ContactId && other.Version.Value == version.Version.Value - 1);
        return previous is null || previous.OrganizationId != version.OrganizationId;
    }

    /// <summary>
    /// Says whether a user existed and was active at a moment, from their own versions up to that moment.
    /// </summary>
    /// <param name="versions">Every version of every user.</param>
    /// <param name="user">The user to ask about.</param>
    /// <param name="moment">The moment to ask about.</param>
    /// <returns><see langword="true"/> when the user had been created and was not archived.</returns>
    private static bool UserWasActive(UserVersion[] versions, UserId user, DateTime moment)
    {
        UserVersion? stateThen = versions
            .Where(version => version.UserId == user && version.ChangedOn <= moment)
            .OrderBy(static version => version.Version.Value)
            .LastOrDefault();
        return stateThen is not null && stateThen.ArchivedOn is null;
    }

    /// <summary>
    /// Says whether an organization existed and was active at a moment, from its own versions up to that moment.
    /// </summary>
    /// <param name="versions">Every version of every organization.</param>
    /// <param name="organization">The organization to ask about.</param>
    /// <param name="moment">The moment to ask about.</param>
    /// <returns><see langword="true"/> when the organization had been created and was not archived.</returns>
    private static bool OrganizationWasActive(
        OrganizationVersion[] versions,
        OrganizationId organization,
        DateTime moment)
    {
        OrganizationVersion? stateThen = versions
            .Where(version => version.OrganizationId == organization && version.ChangedOn <= moment)
            .OrderBy(static version => version.Version.Value)
            .LastOrDefault();
        return stateThen is not null && stateThen.ArchivedOn is null;
    }

    #endregion Private Methods
}
