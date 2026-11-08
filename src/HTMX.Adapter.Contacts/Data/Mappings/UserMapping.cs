namespace HTMX.Adapter.Contacts.Data.Mappings;

using System;
using System.Linq.Expressions;
using HTMX.Adapter.Contacts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Maps <see cref="User"/> to the Users table.
/// </summary>
internal sealed class UserMapping : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// Names the table, makes usernames and email addresses unique however they are capitalized, makes the version
    /// number refuse a save from an out-of-date form, and points the three "by" columns at other users.
    /// </summary>
    /// <param name="builder">The builder for the user entity.</param>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Users");
        builder.HasKey(static user => user.Id);

        // AUTOINCREMENT, which Entity Framework leaves off a key it converts: a deleted user's identifier is never
        // given out again, so an old link can never open a different user.
        builder.Property(static user => user.Id).UseAutoincrement();

        // SQLite compares text case-sensitively unless told otherwise; NOCASE makes Pat@example.com and
        // pat@example.com the same address, here and in the unique indexes. It folds A to Z only, not accented letters.
        builder.Property(static user => user.Username).UseCollation("NOCASE");
        builder.Property(static user => user.Email).UseCollation("NOCASE");
        builder.HasIndex(static user => user.Username).IsUnique();
        builder.HasIndex(static user => user.Email).IsUnique();

        // Every update says which version it started from, and saves nothing if another save got there first.
        builder.Property(static user => user.Version).IsConcurrencyToken();

        ReferToUser(builder, static user => user.CreatedBy);
        ReferToUser(builder, static user => user.LastModifiedBy);
        ReferToUser(builder, static user => user.ArchivedBy);
    }

    /// <summary>
    /// Makes a column a foreign key to another user, which cannot be deleted while this user points at them.
    /// </summary>
    /// <param name="builder">The builder for the user entity.</param>
    /// <param name="foreignKey">The column that holds the other user's identifier.</param>
    private static void ReferToUser(EntityTypeBuilder<User> builder, Expression<Func<User, object?>> foreignKey)
    {
        ReferenceCollectionBuilder<User, User> reference = builder.HasOne<User>().WithMany();
        reference.HasForeignKey(foreignKey);
        reference.OnDelete(DeleteBehavior.Restrict);
    }
}
