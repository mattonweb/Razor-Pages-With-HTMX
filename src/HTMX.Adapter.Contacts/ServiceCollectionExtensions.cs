namespace HTMX.Adapter.Contacts;

using System;
using HTMX.Adapter.Contacts.Configuration;
using HTMX.Adapter.Contacts.Data;
using HTMX.Adapter.Contacts.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Registers the contacts adapter: the one public way into it, besides its options and its contracts.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory database, the context that reads and writes it, and the service that creates its
    /// tables and writes the seed when the application starts.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="options">What the host supplies: the database's name.</param>
    /// <returns>The same service collection, so registrations can follow one another.</returns>
    public static IServiceCollection AddContactsAdapter(this IServiceCollection services, ContactsOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        // The system clock, unless the host or a test has registered another one first.
        services.TryAddSingleton(TimeProvider.System);

        // A singleton made by a factory, so the container disposes it, and so the database, when the application stops.
        services.AddSingleton(_ => new InMemoryDatabase(options.DatabaseName));
        services.AddDbContext<ContactsDatabaseContext>(ConfigureContext);
        services.AddSingleton<SeedWriter>();
        services.AddHostedService<DatabaseCreator>();
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
