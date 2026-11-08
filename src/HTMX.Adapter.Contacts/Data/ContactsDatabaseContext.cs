namespace HTMX.Adapter.Contacts.Data;

using System;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The contacts database's Entity Framework context. One instance serves one request and is then discarded, so nothing
/// it has read outlives the request that read it.
/// </summary>
/// <remarks>
/// Pending: the entities, starting with the user, are mapped here as each arrives.
/// </remarks>
internal sealed class ContactsDatabaseContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContactsDatabaseContext"/> class.
    /// </summary>
    /// <param name="options">The options that point the context at its database.</param>
    public ContactsDatabaseContext(DbContextOptions<ContactsDatabaseContext> options)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(options);
    }
}
