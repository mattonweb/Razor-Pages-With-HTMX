namespace HTMX.Test.Unit.Adapter.Customers.Data;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers;
using HTMX.Adapter.Customers.Configuration;
using HTMX.Adapter.Customers.Data;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="DatabaseCreator"/> class, registered as the
/// application registers it, through <see cref="ServiceCollectionExtensions.AddCustomersAdapter"/>.
/// </summary>
[TestClass]
public sealed class DatabaseCreatorTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that starting the adapter creates the tables and writes the seed, as the application does
    /// before its first request.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_NewDatabase_WritesTheSeed()
    {
        // Arrange (Given)
        int users;

        // Act (When)
        using (ServiceProvider services = CreateServices())
        {
            IHostedService creator = services.GetRequiredService<IHostedService>();
            await creator.StartAsync(CancellationToken.None).ConfigureAwait(false);
            users = await CountUsersAsync(services).ConfigureAwait(false);
        }

        // Assert (Then)
        Assert.AreEqual(
            49,
            users,
            "Starting the adapter should leave the 49 seed users in the database.");
    }

    /// <summary>
    /// Unit test to verify that starting the adapter again leaves the database as it is, rather than failing on seed
    /// users that already exist.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_CalledTwice_WritesTheSeedOnce()
    {
        // Arrange (Given)
        int users;

        // Act (When)
        using (ServiceProvider services = CreateServices())
        {
            IHostedService creator = services.GetRequiredService<IHostedService>();
            await creator.StartAsync(CancellationToken.None).ConfigureAwait(false);
            await creator.StartAsync(CancellationToken.None).ConfigureAwait(false);
            users = await CountUsersAsync(services).ConfigureAwait(false);
        }

        // Assert (Then)
        Assert.AreEqual(
            49,
            users,
            "A second start should leave the 49 seed users as they were.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Registers the adapter as the application does, over a database no other test uses, with the clock stopped at
    /// the tests' start time.
    /// </summary>
    /// <returns>The services, which the caller disposes, and the database with them.</returns>
    private static ServiceProvider CreateServices()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(TestDatabase.CreateClock());
        CustomersOptions options = new CustomersOptions
        {
            DatabaseName = new DatabaseName(Guid.NewGuid().ToString("N")),
        };
        services.AddCustomersAdapter(options);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Counts the users through a context from a scope of its own, as one request would.
    /// </summary>
    /// <param name="services">The adapter's services.</param>
    /// <returns>The number of users.</returns>
    private static async Task<int> CountUsersAsync(ServiceProvider services)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            CustomersDatabaseContext context = scope.ServiceProvider.GetRequiredService<CustomersDatabaseContext>();
            return await context.Set<User>().CountAsync().ConfigureAwait(false);
        }
    }

    #endregion Private Methods
}
