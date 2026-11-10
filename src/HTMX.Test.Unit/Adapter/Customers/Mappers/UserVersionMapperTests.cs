namespace HTMX.Test.Unit.Adapter.Customers.Mappers;

using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data;
using HTMX.Adapter.Customers.Data.Seed;
using HTMX.Adapter.Customers.Entities;
using HTMX.Adapter.Customers.Mappers;
using HTMX.Test.Unit.Adapter.Customers.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="UserVersionMapper"/> class: what deleting a user
/// does to the histories that name them.
/// </summary>
[TestClass]
public sealed class UserVersionMapperTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a user who appears in another user's history cannot be deleted, so the history never
    /// loses who made a change. Sam Sample archived Pat Example.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_DeleteUserWhoChangedAnother_ThrowsDbUpdateException()
    {
        // Arrange (Given)
        bool exceptionThrown = false;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            try
            {
                await DeleteUserAsync(database, UserSeed.SamSample).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                exceptionThrown = true;
            }
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Deleting Sam Sample should be refused with DbUpdateException.");
    }

    /// <summary>
    /// Unit test to verify that a user who changed no one else can be deleted, and their own history goes with them.
    /// Casey Mockup is archived and has changed nothing.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_DeleteUserWhoChangedNoOne_DeletesTheirHistory()
    {
        // Arrange (Given)
        UserId casey = new UserId(10002);
        int versionsLeft;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            await DeleteUserAsync(database, casey).ConfigureAwait(false);
            versionsLeft = await CountVersionsAsync(database, casey).ConfigureAwait(false);
        }

        // Assert (Then)
        Assert.AreEqual(
            0,
            versionsLeft,
            "Casey Mockup's two versions should be deleted with the user.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a database with its tables and the seed in them, with the clock stopped at the tests' start time.
    /// </summary>
    /// <returns>The database, which the caller disposes.</returns>
    private static Task<InMemoryDatabase> CreateSeededDatabaseAsync()
    {
        return TestDatabase.CreateSeededAsync(TestDatabase.CreateClock());
    }

    /// <summary>
    /// Deletes a user through a context of its own, as one request would.
    /// </summary>
    /// <param name="database">The database to delete from.</param>
    /// <param name="id">The user to delete.</param>
    /// <returns>A task that completes when the delete is saved.</returns>
    private static async Task DeleteUserAsync(InMemoryDatabase database, UserId id)
    {
        using (CustomersDatabaseContext context = TestDatabase.CreateContext(database))
        {
            DbSet<User> users = context.Set<User>();
            User user = await users.SingleAsync(user => user.Id == id).ConfigureAwait(false);
            users.Remove(user);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Counts one user's versions through a context of its own.
    /// </summary>
    /// <param name="database">The database to read.</param>
    /// <param name="id">The user whose versions to count.</param>
    /// <returns>The number of versions.</returns>
    private static async Task<int> CountVersionsAsync(InMemoryDatabase database, UserId id)
    {
        using (CustomersDatabaseContext context = TestDatabase.CreateContext(database))
        {
            DbSet<UserVersion> versions = context.Set<UserVersion>();
            return await versions.CountAsync(version => version.UserId == id).ConfigureAwait(false);
        }
    }

    #endregion Private Methods
}
