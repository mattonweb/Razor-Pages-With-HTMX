namespace HTMX.Test.Unit.Adapter.Contacts.Data;

using System;
using System.Threading.Tasks;
using HTMX.Adapter.Contacts.Configuration;
using HTMX.Adapter.Contacts.Data;
using HTMX.Adapter.Contacts.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Time.Testing;

/// <summary>
/// Builds the databases the tests read and write: each one new, under a name no other test uses, so the tests can
/// run side by side.
/// </summary>
internal static class TestDatabase
{
    /// <summary>
    /// Gets the moment the tests' application starts, so every seeded date is a known time before it.
    /// </summary>
    internal static DateTime StartedOn { get; } = new DateTime(2026, 11, 7, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Creates a clock stopped at <see cref="StartedOn"/>.
    /// </summary>
    /// <returns>The clock.</returns>
    internal static FakeTimeProvider CreateClock()
    {
        return new FakeTimeProvider(new DateTimeOffset(StartedOn));
    }

    /// <summary>
    /// Creates a database with its tables and the seed in them, as the application starts with.
    /// </summary>
    /// <param name="clock">The clock the seed writer reads.</param>
    /// <returns>The database, which the caller disposes.</returns>
    internal static async Task<InMemoryDatabase> CreateSeededAsync(TimeProvider clock)
    {
        InMemoryDatabase database = new InMemoryDatabase(new DatabaseName(Guid.NewGuid().ToString("N")));
        using (ContactsDatabaseContext context = CreateContext(database))
        {
            DatabaseFacade facade = context.Database;
            await facade.EnsureCreatedAsync().ConfigureAwait(false);
            SeedWriter writer = new SeedWriter(clock);
            await writer.WriteAsync(context).ConfigureAwait(false);
        }

        return database;
    }

    /// <summary>
    /// Creates a context on the database, as one request would have.
    /// </summary>
    /// <param name="database">The database the context reads and writes.</param>
    /// <returns>A new context, which the caller disposes.</returns>
    internal static ContactsDatabaseContext CreateContext(InMemoryDatabase database)
    {
        DbContextOptionsBuilder<ContactsDatabaseContext> options = new DbContextOptionsBuilder<ContactsDatabaseContext>();
        database.Configure(options);
        return new ContactsDatabaseContext(options.Options);
    }
}
