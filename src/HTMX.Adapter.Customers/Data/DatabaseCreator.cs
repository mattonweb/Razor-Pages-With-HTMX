namespace HTMX.Adapter.Customers.Data;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Creates the database's tables from the entity model, and writes the seed into them, when the application asks.
/// </summary>
/// <remarks>
/// <para>
/// The application calls it once, as it starts and before it runs, so the seed is in place before the first request
/// arrives. It is an ordinary service, not a hosted one: the host is the application's, and so is the decision of when
/// the database is made.
/// </para>
/// <para>
/// The seed is written through a context built here rather than one from dependency injection, because only a context
/// from dependency injection logs its SQL. The seed is over 2,000 inserts; written quietly, it leaves the console to
/// the SQL each request runs.
/// </para>
/// </remarks>
internal sealed class DatabaseCreator : ICustomersDatabase
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

    /// <inheritdoc/>
    public async Task CreateAsync(CancellationToken cancellationToken = default)
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
}
