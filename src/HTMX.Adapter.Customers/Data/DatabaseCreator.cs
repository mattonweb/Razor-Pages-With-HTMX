namespace HTMX.Adapter.Customers.Data;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Creates the database's tables from the entity model when the application starts, and writes the seed into them.
/// </summary>
/// <remarks>
/// <para>
/// A web application starts its hosted services before its web server, so the seed is in place before the first
/// request arrives.
/// </para>
/// <para>
/// The seed is written through a context built here rather than one from dependency injection, because only a context
/// from dependency injection logs its SQL. The seed is over 2,000 inserts; written quietly, it leaves the console to
/// the SQL each request runs.
/// </para>
/// </remarks>
internal sealed class DatabaseCreator : IHostedService
{
    private readonly InMemoryDatabase _database;
    private readonly SeedWriter _seedWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseCreator"/> class.
    /// </summary>
    /// <param name="database">The database to create the tables in.</param>
    /// <param name="seedWriter">Writes the seed into the new tables.</param>
    public DatabaseCreator(InMemoryDatabase database, SeedWriter seedWriter)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(seedWriter);
        this._database = database;
        this._seedWriter = seedWriter;
    }

    /// <summary>
    /// Creates the tables and writes the seed. A database that already has tables is left as it is.
    /// </summary>
    /// <param name="cancellationToken">Signals that the application is stopping before it has started.</param>
    /// <returns>A task that completes when the seed is saved.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        DbContextOptionsBuilder<CustomersDatabaseContext> options = new DbContextOptionsBuilder<CustomersDatabaseContext>();
        this._database.Configure(options);
        using (CustomersDatabaseContext context = new CustomersDatabaseContext(options.Options))
        {
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
