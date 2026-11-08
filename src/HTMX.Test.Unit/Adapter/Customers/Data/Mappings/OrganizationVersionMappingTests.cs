namespace HTMX.Test.Unit.Adapter.Customers.Data.Mappings;

using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data;
using HTMX.Adapter.Customers.Data.Mappings;
using HTMX.Adapter.Customers.Data.Seed;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="OrganizationVersionMapping"/> class: what deleting
/// an organization does to its history.
/// </summary>
[TestClass]
public sealed class OrganizationVersionMappingTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that an organization's history is deleted with it. Draft Distribution is archived, and
    /// nothing belongs to it.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_DeleteOrganization_DeletesItsHistory()
    {
        // Arrange (Given)
        OrganizationId draftDistribution = OrganizationSeed.DraftDistribution;
        int versionsLeft;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            await DeleteOrganizationAsync(database, draftDistribution).ConfigureAwait(false);
            versionsLeft = await CountVersionsAsync(database, draftDistribution).ConfigureAwait(false);
        }

        // Assert (Then)
        Assert.AreEqual(
            0,
            versionsLeft,
            "Draft Distribution's two versions should be deleted with it.");
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
    /// Deletes an organization through a context of its own, as one request would.
    /// </summary>
    /// <param name="database">The database to delete from.</param>
    /// <param name="id">The organization to delete.</param>
    /// <returns>A task that completes when the delete is saved.</returns>
    private static async Task DeleteOrganizationAsync(InMemoryDatabase database, OrganizationId id)
    {
        using (CustomersDatabaseContext context = TestDatabase.CreateContext(database))
        {
            DbSet<Organization> organizations = context.Set<Organization>();
            Organization organization = await organizations
                .SingleAsync(organization => organization.Id == id)
                .ConfigureAwait(false);
            organizations.Remove(organization);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Counts one organization's versions through a context of its own.
    /// </summary>
    /// <param name="database">The database to read.</param>
    /// <param name="id">The organization whose versions to count.</param>
    /// <returns>The number of versions.</returns>
    private static async Task<int> CountVersionsAsync(InMemoryDatabase database, OrganizationId id)
    {
        using (CustomersDatabaseContext context = TestDatabase.CreateContext(database))
        {
            DbSet<OrganizationVersion> versions = context.Set<OrganizationVersion>();
            return await versions.CountAsync(version => version.OrganizationId == id).ConfigureAwait(false);
        }
    }

    #endregion Private Methods
}
