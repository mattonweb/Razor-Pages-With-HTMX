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
/// Contains unit tests for verifying the behavior of the <see cref="ContactVersionMapping"/> class: what deleting a
/// contact, or an organization a contact once belonged to, does to the history.
/// </summary>
[TestClass]
public sealed class ContactVersionMappingTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a contact's history is deleted with them. Darcy Dataset is archived, and nothing else
    /// names them.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_DeleteContact_DeletesTheirHistory()
    {
        // Arrange (Given)
        ContactId darcy = ContactSeed.DarcyDataset;
        int versionsLeft;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            await DeleteContactAsync(database, darcy).ConfigureAwait(false);
            versionsLeft = await CountVersionsAsync(database, darcy).ConfigureAwait(false);
        }

        // Assert (Then)
        Assert.AreEqual(
            0,
            versionsLeft,
            "Darcy Dataset's two versions should be deleted with the contact.");
    }

    /// <summary>
    /// Unit test to verify that an organization no contact belongs to any more can be deleted, although an old version
    /// of a contact still names it. Ainsley Archetype belonged to Fictional Freight before moving.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_DeleteOrganizationNamedOnlyInHistory_DeletesIt()
    {
        // Arrange (Given)
        OrganizationId fictionalFreight = new OrganizationId(10002);
        int organizationsLeft;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            await DeleteOrganizationAsync(database, fictionalFreight).ConfigureAwait(false);
            organizationsLeft = await CountOrganizationsAsync(database, fictionalFreight).ConfigureAwait(false);
        }

        // Assert (Then)
        Assert.AreEqual(
            0,
            organizationsLeft,
            "Fictional Freight should be deleted although Ainsley Archetype's history names it.");
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
    /// Deletes a contact through a context of its own, as one request would.
    /// </summary>
    /// <param name="database">The database to delete from.</param>
    /// <param name="id">The contact to delete.</param>
    /// <returns>A task that completes when the delete is saved.</returns>
    private static async Task DeleteContactAsync(InMemoryDatabase database, ContactId id)
    {
        using (CustomersDatabaseContext context = TestDatabase.CreateContext(database))
        {
            DbSet<Contact> contacts = context.Set<Contact>();
            Contact contact = await contacts.SingleAsync(contact => contact.Id == id).ConfigureAwait(false);
            contacts.Remove(contact);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
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
    /// Counts one contact's versions through a context of its own.
    /// </summary>
    /// <param name="database">The database to read.</param>
    /// <param name="id">The contact whose versions to count.</param>
    /// <returns>The number of versions.</returns>
    private static async Task<int> CountVersionsAsync(InMemoryDatabase database, ContactId id)
    {
        using (CustomersDatabaseContext context = TestDatabase.CreateContext(database))
        {
            DbSet<ContactVersion> versions = context.Set<ContactVersion>();
            return await versions.CountAsync(version => version.ContactId == id).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Counts the organizations with one identifier, which is one before a delete and none after it.
    /// </summary>
    /// <param name="database">The database to read.</param>
    /// <param name="id">The organization to count.</param>
    /// <returns>The number of organizations with that identifier.</returns>
    private static async Task<int> CountOrganizationsAsync(InMemoryDatabase database, OrganizationId id)
    {
        using (CustomersDatabaseContext context = TestDatabase.CreateContext(database))
        {
            DbSet<Organization> organizations = context.Set<Organization>();
            return await organizations.CountAsync(organization => organization.Id == id).ConfigureAwait(false);
        }
    }

    #endregion Private Methods
}
