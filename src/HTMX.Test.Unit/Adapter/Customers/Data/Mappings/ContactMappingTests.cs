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
/// Contains unit tests for verifying the behavior of the <see cref="ContactMapping"/> class: the rules the Contacts
/// table enforces on every save.
/// </summary>
[TestClass]
public sealed class ContactMappingTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a contact cannot be saved in an organization that does not exist, so every contact
    /// belongs to a real one.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_UnknownOrganization_ThrowsDbUpdateException()
    {
        // Arrange (Given)
        Contact stray = CreateNewContact(new OrganizationId(99999));
        bool exceptionThrown = false;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            try
            {
                await AddContactAsync(database, stray).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                exceptionThrown = true;
            }
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A contact in an organization that does not exist should be refused with DbUpdateException.");
    }

    /// <summary>
    /// Unit test to verify that a contact created after the seed carries on from the seed's highest identifier.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_NewContactAfterSeed_GetsTheNextIdentifier()
    {
        // Arrange (Given)
        Contact newcomer = CreateNewContact(OrganizationSeed.PlaceholderLogistics);
        Contact saved;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            saved = await AddContactAsync(database, newcomer).ConfigureAwait(false);
        }

        // Assert (Then)
        Assert.AreEqual(
            11501L,
            saved.Id.Value,
            "The first contact created after the seed should be 11501.");
    }

    /// <summary>
    /// Unit test to verify that an organization cannot be deleted while contacts belong to it, even when it is
    /// archived. Bellamy Benchmark and Carmen Casefile belong to Mockup Media.
    /// </summary>
    [TestMethod]
    public async Task SaveChanges_DeleteOrganizationWithContacts_ThrowsDbUpdateException()
    {
        // Arrange (Given)
        bool exceptionThrown = false;

        // Act (When)
        using (InMemoryDatabase database = await CreateSeededDatabaseAsync().ConfigureAwait(false))
        {
            try
            {
                await DeleteOrganizationAsync(database, OrganizationSeed.MockupMedia).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                exceptionThrown = true;
            }
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Deleting Mockup Media should be refused with DbUpdateException.");
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
    /// Creates a contact who is not yet saved, as Demo User would create them.
    /// </summary>
    /// <param name="organization">The organization the contact belongs to.</param>
    /// <returns>The unsaved contact.</returns>
    private static Contact CreateNewContact(OrganizationId organization)
    {
        return new Contact
        {
            Name = new DisplayName("Nova Newcomer"),
            OrganizationId = organization,
            CreatedOn = TestDatabase.StartedOn,
            CreatedBy = UserSeed.DemoUser,
            LastModifiedOn = TestDatabase.StartedOn,
            LastModifiedBy = UserSeed.DemoUser,
            Version = new VersionNumber(1),
        };
    }

    /// <summary>
    /// Saves a new contact through a context of its own, as one request would.
    /// </summary>
    /// <param name="database">The database to save into.</param>
    /// <param name="contact">The contact to save.</param>
    /// <returns>The contact as saved, with the identifier the database gave them.</returns>
    private static async Task<Contact> AddContactAsync(InMemoryDatabase database, Contact contact)
    {
        using (CustomersDatabaseContext context = TestDatabase.CreateContext(database))
        {
            context.Set<Contact>().Add(contact);
            await context.SaveChangesAsync().ConfigureAwait(false);
            return contact;
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

    #endregion Private Methods
}
