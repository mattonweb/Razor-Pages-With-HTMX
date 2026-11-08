namespace HTMX.Adapter.Contacts.Data.Mappings;

using System;
using System.Linq.Expressions;
using HTMX.Adapter.Contacts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Maps <see cref="UserVersion"/> to the UserVersions table.
/// </summary>
internal sealed class UserVersionMapping : IEntityTypeConfiguration<UserVersion>
{
    /// <summary>
    /// Names the table, keys each version by its user and number, stores the kind of change by name so the SQL log
    /// reads well, and points the version at its user and at whoever made the change.
    /// </summary>
    /// <param name="builder">The builder for the user version entity.</param>
    public void Configure(EntityTypeBuilder<UserVersion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("UserVersions");
        builder.HasKey(static version => new { version.UserId, version.Version });
        builder.Property(static version => version.Change).HasConversion<string>();

        // A user's history goes with them when they are deleted; a user who made a change in someone else's history
        // cannot be deleted at all.
        ReferToUser(builder, static version => version.UserId, DeleteBehavior.Cascade);
        ReferToUser(builder, static version => version.ChangedBy, DeleteBehavior.Restrict);
        ReferToUser(builder, static version => version.ArchivedBy, DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Makes a column a foreign key to a user.
    /// </summary>
    /// <param name="builder">The builder for the user version entity.</param>
    /// <param name="foreignKey">The column that holds the user's identifier.</param>
    /// <param name="onDelete">What deleting that user does to this version.</param>
    private static void ReferToUser(
        EntityTypeBuilder<UserVersion> builder,
        Expression<Func<UserVersion, object?>> foreignKey,
        DeleteBehavior onDelete)
    {
        ReferenceCollectionBuilder<User, UserVersion> reference = builder.HasOne<User>().WithMany();
        reference.HasForeignKey(foreignKey);
        reference.OnDelete(onDelete);
    }
}
