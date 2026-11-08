namespace HTMX.Adapter.Contacts.Data;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Creates the database's tables from the entity model when the application starts.
/// </summary>
/// <remarks>
/// A web application starts its hosted services before its web server, so the tables exist before the first
/// request arrives.
/// </remarks>
internal sealed class DatabaseCreator : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseCreator"/> class.
    /// </summary>
    /// <param name="scopeFactory">Creates the scope the context is resolved from, as a request would.</param>
    public DatabaseCreator(IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        this._scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Creates the tables.
    /// </summary>
    /// <param name="cancellationToken">Signals that the application is stopping before it has started.</param>
    /// <returns>A task that completes when the tables exist.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (IServiceScope scope = this._scopeFactory.CreateScope())
        {
            ContactsDatabaseContext context = scope.ServiceProvider.GetRequiredService<ContactsDatabaseContext>();
            DatabaseFacade database = context.Database;
            await database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
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
