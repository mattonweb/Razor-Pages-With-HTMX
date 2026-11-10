namespace HTMX.Adapter.Customers.Mappers;

using System;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Maps <see cref="UserVersion"/> to the UserVersions table.
/// </summary>
internal sealed class UserVersionMapper : IEntityTypeConfiguration<UserVersion>
{
    /// <summary>
    /// Names the table, keys each version by its user and number, and points the version at its user and at whoever
    /// made the change.
    /// </summary>
    /// <param name="builder">The builder for the user version entity.</param>
    public void Configure(EntityTypeBuilder<UserVersion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("UserVersions");
        builder.HasKey(static version => new { version.UserId, version.Version });

        // A user's history goes with them when they are deleted; a user who made a change in someone else's history
        // cannot be deleted at all.
        UserReferences.Add(builder, static version => version.UserId, DeleteBehavior.Cascade);
        UserReferences.Add(builder, static version => version.ChangedBy, DeleteBehavior.Restrict);
        UserReferences.Add(builder, static version => version.ArchivedBy, DeleteBehavior.Restrict);
    }
}
