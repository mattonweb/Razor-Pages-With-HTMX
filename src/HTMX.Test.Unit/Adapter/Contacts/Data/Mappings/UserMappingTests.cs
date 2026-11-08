namespace HTMX.Test.Unit.Adapter.Contacts.Data.Mappings;

using System.Threading.Tasks;
using HTMX.Adapter.Contacts.Contracts;
using HTMX.Adapter.Contacts.Data;
using HTMX.Adapter.Contacts.Data.Mappings;
using HTMX.Adapter.Contacts.Data.Seed;
using HTMX.Adapter.Contacts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="UserMapping"/> class: the rules the Users table
/// enforces on every save.
/// </summary>
[TestClass]
public sealed class UserMappingTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a username that differs from an existing one only in capitals is refused, as MariaDB's
    /// case-insensitive collation would refuse it.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_UsernameDiffersOnlyInCase_ThrowsDbUpdateException()
    {
        // Arrange (Given)
        User duplicate = CreateNewUser(new Username("PAT.EXAMPLE"), new EmailAddress("nova@example.com"));
        bool exceptionThrown = false;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            try
            {
                await AddUserAsync(database, duplicate).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                exceptionThrown = true;
            }
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A username that differs only in capitals should be refused with DbUpdateException.");
    }

    /// <summary>
    /// Unit test to verify that an email address that differs from an existing one only in capitals is refused.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_EmailDiffersOnlyInCase_ThrowsDbUpdateException()
    {
        // Arrange (Given)
        User duplicate = CreateNewUser(new Username("nova.newcomer"), new EmailAddress("Pat@Example.com"));
        bool exceptionThrown = false;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            try
            {
                await AddUserAsync(database, duplicate).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                exceptionThrown = true;
            }
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "An email address that differs only in capitals should be refused with DbUpdateException.");
    }

    /// <summary>
    /// Unit test to verify that a user created after the seed carries on from the seed's highest identifier, so every
    /// identifier stays five digits wide.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_NewUserAfterSeed_GetsTheNextIdentifier()
    {
        // Arrange (Given)
        User newcomer = CreateNewUser(new Username("nova.newcomer"), new EmailAddress("nova@example.com"));
        User saved;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            saved = await AddUserAsync(database, newcomer).ConfigureAwait(false);
        }

        // Assert (Then)
        Assert.AreEqual(
            10050L,
            saved.Id.Value,
            "The first user created after the seed should be 10050.");
    }

    /// <summary>
    /// Unit test to verify that a deleted user's identifier is never given out again, so an old link can never open a
    /// different user.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_NewestUserDeleted_DoesNotReuseTheIdentifier()
    {
        // Arrange (Given)
        User deleted = CreateNewUser(new Username("nova.newcomer"), new EmailAddress("nova@example.com"));
        User replacement = CreateNewUser(new Username("orla.overwrite"), new EmailAddress("orla@example.com"));
        User saved;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            await AddUserAsync(database, deleted).ConfigureAwait(false);
            await DeleteUserAsync(database, deleted).ConfigureAwait(false);
            saved = await AddUserAsync(database, replacement).ConfigureAwait(false);
        }

        // Assert (Then)
        Assert.AreEqual(
            10051L,
            saved.Id.Value,
            "The user created after 10050 was deleted should be 10051, not 10050 again.");
    }

    /// <summary>
    /// Unit test to verify that a save started from a version someone else has already replaced is refused, so one
    /// person's edit never silently overwrites another's.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_StaleVersion_ThrowsDbUpdateConcurrencyException()
    {
        // Arrange (Given)
        bool exceptionThrown = false;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            using (ContactsDatabaseContext first = TestDatabase.CreateContext(database))
            {
                using (ContactsDatabaseContext second = TestDatabase.CreateContext(database))
                {
                    User patInFirst = await FindPatAsync(first).ConfigureAwait(false);
                    User patInSecond = await FindPatAsync(second).ConfigureAwait(false);
                    await RenameAsync(first, patInFirst, new DisplayName("Pat Saved First")).ConfigureAwait(false);
                    try
                    {
                        await RenameAsync(second, patInSecond, new DisplayName("Pat Saved Second")).ConfigureAwait(false);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        exceptionThrown = true;
                    }
                }
            }
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "The second save from version 5 should be refused with DbUpdateConcurrencyException.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a database with its tables and the seed in them, with the clock at the seed's date.
    /// </summary>
    /// <returns>The database, which the caller disposes.</returns>
    private static Task<InMemoryDatabase> CreateSeededDatabaseAsync()
    {
        return TestDatabase.CreateSeededAsync(TestDatabase.CreateClockAtSeedDate());
    }

    /// <summary>
    /// Creates a user who is not yet saved, as Demo User would create them.
    /// </summary>
    /// <param name="username">The new user's name.</param>
    /// <param name="email">The new user's email address.</param>
    /// <returns>The unsaved user.</returns>
    private static User CreateNewUser(Username username, EmailAddress email)
    {
        return new User
        {
            Username = username,
            Email = email,
            DisplayName = new DisplayName("Nova Newcomer"),
            CreatedOn = UserSeed.WrittenOn,
            CreatedBy = UserSeed.DemoUser,
            LastModifiedOn = UserSeed.WrittenOn,
            LastModifiedBy = UserSeed.DemoUser,
            Version = new VersionNumber(1),
        };
    }

    /// <summary>
    /// Saves a new user through a context of its own, as one request would.
    /// </summary>
    /// <param name="database">The database to save into.</param>
    /// <param name="user">The user to save.</param>
    /// <returns>The user as saved, with the identifier the database gave them.</returns>
    private static async Task<User> AddUserAsync(InMemoryDatabase database, User user)
    {
        using (ContactsDatabaseContext context = TestDatabase.CreateContext(database))
        {
            context.Set<User>().Add(user);
            await context.SaveChangesAsync().ConfigureAwait(false);
            return user;
        }
    }

    /// <summary>
    /// Deletes a saved user through a context of its own, as one request would.
    /// </summary>
    /// <param name="database">The database to delete from.</param>
    /// <param name="user">The user, as saved.</param>
    /// <returns>A task that completes when the delete is saved.</returns>
    private static async Task DeleteUserAsync(InMemoryDatabase database, User user)
    {
        using (ContactsDatabaseContext context = TestDatabase.CreateContext(database))
        {
            context.Set<User>().Remove(user);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads Pat Example through a context, which then tracks the row it read.
    /// </summary>
    /// <param name="context">The context to read through.</param>
    /// <returns>Pat Example, at version 5.</returns>
    private static Task<User> FindPatAsync(ContactsDatabaseContext context)
    {
        return context.Set<User>().SingleAsync(static user => user.Id == UserSeed.PatExample);
    }

    /// <summary>
    /// Saves a new display name over the version the context read, as an edit form would.
    /// </summary>
    /// <param name="context">The context that read the user.</param>
    /// <param name="user">The user as the context read them.</param>
    /// <param name="displayName">The new display name.</param>
    /// <returns>A task that completes when the save does.</returns>
    private static async Task RenameAsync(ContactsDatabaseContext context, User user, DisplayName displayName)
    {
        User renamed = new User
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = displayName,
            CreatedOn = user.CreatedOn,
            CreatedBy = user.CreatedBy,
            LastModifiedOn = user.LastModifiedOn,
            LastModifiedBy = user.LastModifiedBy,
            ArchivedOn = user.ArchivedOn,
            ArchivedBy = user.ArchivedBy,
            Version = new VersionNumber(user.Version.Value + 1),
        };
        EntityEntry<User> entry = context.Entry(user);
        entry.CurrentValues.SetValues(renamed);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    #endregion Private Methods
}
