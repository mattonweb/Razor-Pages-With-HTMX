namespace HTMX.Test.Unit.Adapter.Customers.Providers;

using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data.Seed;
using HTMX.Adapter.Customers.Entities;
using HTMX.Adapter.Customers.Providers;
using HTMX.Test.Unit.Adapter.Customers.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="OrganizationProvider"/> class where the service's
/// tests cannot reach: a save that another request's save overtakes between the read and the write.
/// </summary>
[TestClass]
public sealed class OrganizationProviderTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a change is not saved when another request saved the organization after this one read
    /// it: the version number is a concurrency token, so the UPDATE matches no row, and the version that would have
    /// recorded the change is not written either.
    /// </summary>
    [TestMethod]
    public async Task SaveChangeAsync_SavedByAnotherRequestSinceTheRead_SavesNothing()
    {
        // Arrange (Given)
        OrganizationId id = OrganizationSeed.PlaceholderLogistics;
        using (ServiceProvider services = await TestServices.CreateStartedAsync().ConfigureAwait(false))
        {
            using (IServiceScope late = services.CreateScope())
            {
                OrganizationProvider provider = late.ServiceProvider.GetRequiredService<OrganizationProvider>();
                Organization? current = await provider.FindForChangeAsync(id).ConfigureAwait(false);
                Assert.IsNotNull(
                    current,
                    "Placeholder Logistics should be found.");
                OrganizationVersion version = CreateVersion(current, "Placeholder Logistics Limited");
                Organization changed = CreateChanged(current, version);
                await SaveFromAnotherRequestAsync(services, id).ConfigureAwait(false);

                // Act (When)
                bool saved = await provider.SaveChangeAsync(current, changed, version).ConfigureAwait(false);
                OrganizationVersion[] history =
                    await OrganizationServiceCalls.ReadHistoryAsync(services, id).ConfigureAwait(false);

                // Assert (Then)
                Assert.IsFalse(
                    saved,
                    "A change read before another request's save should not be saved.");
                Assert.HasCount(
                    3,
                    history,
                    "Only the other request's change should add a version.");
                Assert.AreEqual(
                    "Placeholder Logistics Group",
                    history[^1].Name.Value,
                    "The latest version should be the other request's.");
            }
        }
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Saves a change to the organization through the service in a request of its own, from version 2, as another
    /// browser tab would while this request is still working.
    /// </summary>
    /// <param name="services">The started services.</param>
    /// <param name="id">The organization to change.</param>
    /// <returns>A task that completes when the change is saved.</returns>
    private static async Task SaveFromAnotherRequestAsync(ServiceProvider services, OrganizationId id)
    {
        OrganizationUpdateRequest request = new OrganizationUpdateRequest
        {
            Id = id,
            Version = new VersionNumber(2),
            Name = new OrganizationName("Placeholder Logistics Group"),
            Industry = Industry.Shipping,
            Website = new WebAddress("https://placeholder.example"),
            ChangedBy = UserSeed.TaylorDummy,
        };
        UpdateOutcome outcome = await OrganizationServiceCalls.UpdateAsync(services, request).ConfigureAwait(false);
        Assert.AreEqual(
            UpdateOutcome.Saved,
            outcome,
            "The other request's change should be saved.");
    }

    /// <summary>
    /// Creates the version that would record a new name, as the service would.
    /// </summary>
    /// <param name="current">The organization as it was read.</param>
    /// <param name="name">The new name.</param>
    /// <returns>The version.</returns>
    private static OrganizationVersion CreateVersion(Organization current, string name)
    {
        return new OrganizationVersion
        {
            OrganizationId = current.Id,
            Version = new VersionNumber(current.Version.Value + 1),
            Change = ChangeKind.Edited,
            ChangedOn = current.LastModifiedOn,
            ChangedBy = UserSeed.DemoUser,
            Name = new OrganizationName(name),
            Industry = current.Industry,
            Website = current.Website,
        };
    }

    /// <summary>
    /// Creates the row's new values from the version, as the service would.
    /// </summary>
    /// <param name="current">The organization as it was read.</param>
    /// <param name="version">The version that records the change.</param>
    /// <returns>The row's new values.</returns>
    private static Organization CreateChanged(Organization current, OrganizationVersion version)
    {
        return new Organization
        {
            Id = current.Id,
            Name = version.Name,
            Industry = version.Industry,
            Website = version.Website,
            CreatedOn = current.CreatedOn,
            CreatedBy = current.CreatedBy,
            LastModifiedOn = version.ChangedOn,
            LastModifiedBy = version.ChangedBy,
            Version = version.Version,
        };
    }

    #endregion Private Methods
}
