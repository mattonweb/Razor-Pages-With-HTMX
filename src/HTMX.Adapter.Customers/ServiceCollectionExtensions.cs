namespace HTMX.Adapter.Customers;

using System;
using HTMX.Adapter.Customers.Configuration;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data;
using HTMX.Adapter.Customers.Data.Seed;
using HTMX.Adapter.Customers.Providers;
using HTMX.Adapter.Customers.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Registers the customers adapter: the one public way into it, besides its options and its contracts.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory database, the context that reads and writes it, the service that creates its tables
    /// and writes the seed when the application starts, and the services behind the contracts.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="options">What the host supplies: the database's name.</param>
    /// <returns>The same service collection, so registrations can follow one another.</returns>
    public static IServiceCollection AddCustomersAdapter(this IServiceCollection services, CustomersOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        // The system clock, unless the host or a test has registered another one first.
        services.TryAddSingleton(TimeProvider.System);

        // A singleton made by a factory, so the container disposes it, and so the database, when the application stops.
        services.AddSingleton(_ => new InMemoryDatabase(options.DatabaseName));
        services.AddDbContext<CustomersDatabaseContext>(ConfigureContext);
        services.AddSingleton<SeedWriter>();
        services.AddHostedService<DatabaseCreator>();

        // One of each per request, like the context they read through.
        services.AddScoped<OrganizationProvider>();
        services.AddScoped<OrganizationVersionProvider>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<ContactProvider>();
        services.AddScoped<IContactService, ContactService>();
        return services;
    }

    /// <summary>
    /// Points a new context at the in-memory database.
    /// </summary>
    /// <param name="serviceProvider">The provider the context is being created from.</param>
    /// <param name="options">The options of the context being configured.</param>
    private static void ConfigureContext(IServiceProvider serviceProvider, DbContextOptionsBuilder options)
    {
        InMemoryDatabase database = serviceProvider.GetRequiredService<InMemoryDatabase>();
        database.Configure(options);
    }
}
