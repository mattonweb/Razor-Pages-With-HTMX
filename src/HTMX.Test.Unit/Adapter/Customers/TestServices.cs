namespace HTMX.Test.Unit.Adapter.Customers;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers;
using HTMX.Adapter.Customers.Configuration;
using HTMX.Test.Unit.Adapter.Customers.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Builds the adapter's services as the application registers them, over a seeded database no other test uses, so a
/// test reaches the adapter through its public contracts.
/// </summary>
internal static class TestServices
{
    /// <summary>
    /// Registers the adapter, with the clock stopped at the tests' start time, and starts it, which creates and seeds
    /// the database.
    /// </summary>
    /// <returns>The services, which the caller disposes, and the database with them.</returns>
    internal static async Task<ServiceProvider> CreateStartedAsync()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(TestDatabase.CreateClock());
        CustomersOptions options = new CustomersOptions
        {
            DatabaseName = new DatabaseName(Guid.NewGuid().ToString("N")),
        };
        services.AddCustomersAdapter(options);
        ServiceProvider provider = services.BuildServiceProvider();
        IHostedService creator = provider.GetRequiredService<IHostedService>();
        await creator.StartAsync(CancellationToken.None).ConfigureAwait(false);
        return provider;
    }
}
