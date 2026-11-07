namespace HTMX.Application.Web.Data;

using System;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The application's Entity Framework context. One instance serves one request and is then discarded, so nothing it
/// has read outlives the request that read it.
/// </summary>
/// <remarks>
/// Pending: the entities, starting with the user, are mapped here as each arrives.
/// </remarks>
public sealed class ApplicationDatabaseContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDatabaseContext"/> class.
    /// </summary>
    /// <param name="options">The options that point the context at its database.</param>
    public ApplicationDatabaseContext(DbContextOptions<ApplicationDatabaseContext> options)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(options);
    }
}
