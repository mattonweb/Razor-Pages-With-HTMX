namespace HTMX.Adapter.Contacts.Data;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Contacts.Data.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Creates the database's tables from the entity model when the application starts, and writes the seed into them.
/// </summary>
/// <remarks>
/// A web application starts its hosted services before its web server, so the seed is in place before the first
/// request arrives.
/// </remarks>
internal sealed class DatabaseCreator : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SeedWriter _seedWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseCreator"/> class.
    /// </summary>
    /// <param name="scopeFactory">Creates the scope the context is resolved from, as a request would.</param>
    /// <param name="seedWriter">Writes the seed into the new tables.</param>
    public DatabaseCreator(IServiceScopeFactory scopeFactory, SeedWriter seedWriter)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(seedWriter);
        this._scopeFactory = scopeFactory;
        this._seedWriter = seedWriter;
    }

    /// <summary>
    /// Creates the tables and writes the seed. A database that already has tables is left as it is.
    /// </summary>
    /// <param name="cancellationToken">Signals that the application is stopping before it has started.</param>
    /// <returns>A task that completes when the seed is saved.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (IServiceScope scope = this._scopeFactory.CreateScope())
        {
            ContactsDatabaseContext context = scope.ServiceProvider.GetRequiredService<ContactsDatabaseContext>();
            DatabaseFacade database = context.Database;
            bool created = await database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            if (!created)
            {
                return;
            }

            await this._seedWriter.WriteAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Does nothing: the database is deleted when the application disposes it, not when this service stops.
    /// </summary>
    /// <param name="cancellationToken">Not used.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
