namespace HTMX.Test.Unit.Adapter.Contacts.Data.Mappings;

using System.Threading.Tasks;
using HTMX.Adapter.Contacts.Contracts;
using HTMX.Adapter.Contacts.Data;
using HTMX.Adapter.Contacts.Data.Mappings;
using HTMX.Adapter.Contacts.Data.Seed;
using HTMX.Adapter.Contacts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="OrganizationMapping"/> class: the rules the
/// Organizations table enforces on every save.
/// </summary>
[TestClass]
public sealed class OrganizationMappingTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a name that differs from an existing organization's only in capitals is refused.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_NameDiffersOnlyInCase_ThrowsDbUpdateException()
    {
        // Arrange (Given)
        Organization duplicate = CreateNewOrganization(new OrganizationName("PLACEHOLDER LOGISTICS"));
        bool exceptionThrown = false;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            try
            {
                await AddOrganizationAsync(database, duplicate).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                exceptionThrown = true;
            }
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A name that differs only in capitals should be refused with DbUpdateException.");
    }

    /// <summary>
    /// Unit test to verify that an organization created after the seed carries on from the seed's highest identifier.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_NewOrganizationAfterSeed_GetsTheNextIdentifier()
    {
        // Arrange (Given)
        Organization newcomer = CreateNewOrganization(new OrganizationName("Newcomer Networks"));
        Organization saved;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            saved = await AddOrganizationAsync(database, newcomer).ConfigureAwait(false);
        }

        // Assert (Then)
        Assert.AreEqual(
            10016L,
            saved.Id.Value,
            "The first organization created after the seed should be 10016.");
    }

    /// <summary>
    /// Unit test to verify that a user who created an organization cannot be deleted, even when nothing else names
    /// them. Jamie O'Placeholder's only change is creating Sandbox Software.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_DeleteUserWhoCreatedAnOrganization_ThrowsDbUpdateException()
    {
        // Arrange (Given)
        UserId jamie = new UserId(10012);
        bool exceptionThrown = false;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            try
            {
                await DeleteUserAsync(database, jamie).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                exceptionThrown = true;
            }
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Deleting Jamie O'Placeholder should be refused with DbUpdateException.");
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
    /// Creates an organization that is not yet saved, as Demo User would create it.
    /// </summary>
    /// <param name="name">The new organization's name.</param>
    /// <returns>The unsaved organization.</returns>
    private static Organization CreateNewOrganization(OrganizationName name)
    {
        return new Organization
        {
            Name = name,
            Industry = Industry.Technology,
            CreatedOn = TestDatabase.StartedOn,
            CreatedBy = UserSeed.DemoUser,
            LastModifiedOn = TestDatabase.StartedOn,
            LastModifiedBy = UserSeed.DemoUser,
            Version = new VersionNumber(1),
        };
    }

    /// <summary>
    /// Saves a new organization through a context of its own, as one request would.
    /// </summary>
    /// <param name="database">The database to save into.</param>
    /// <param name="organization">The organization to save.</param>
    /// <returns>The organization as saved, with the identifier the database gave it.</returns>
    private static async Task<Organization> AddOrganizationAsync(InMemoryDatabase database, Organization organization)
    {
        using (ContactsDatabaseContext context = TestDatabase.CreateContext(database))
        {
            context.Set<Organization>().Add(organization);
            await context.SaveChangesAsync().ConfigureAwait(false);
            return organization;
        }
    }

    /// <summary>
    /// Deletes a user through a context of its own, as one request would.
    /// </summary>
    /// <param name="database">The database to delete from.</param>
    /// <param name="id">The user to delete.</param>
    /// <returns>A task that completes when the delete is saved.</returns>
    private static async Task DeleteUserAsync(InMemoryDatabase database, UserId id)
    {
        using (ContactsDatabaseContext context = TestDatabase.CreateContext(database))
        {
            DbSet<User> users = context.Set<User>();
            User user = await users.SingleAsync(user => user.Id == id).ConfigureAwait(false);
            users.Remove(user);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    #endregion Private Methods
}
