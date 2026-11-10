namespace HTMX.Adapter.Customers.Data.Mappings;

using System;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Maps <see cref="User"/> to the Users table.
/// </summary>
internal sealed class UserMapping : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// Names the table, makes usernames and email addresses unique however they are capitalized, sorts names however
    /// they are capitalized, indexes the orders the users list is paged in, makes the version number refuse a save
    /// from an out-of-date form, and points the three "by" columns at other users.
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

        // The users list sorts by name however it is capitalized, and pages by key, so an index holds each order it is
        // read in: the name or the last change, then the identifier that settles a tie. The unique indexes above give
        // the username's and the email address's orders, because SQLite ends every index entry with the identifier.
        builder.Property(static user => user.DisplayName).UseCollation("NOCASE");
        builder.HasIndex(static user => new { user.DisplayName, user.Id });
        builder.HasIndex(static user => new { user.LastModifiedOn, user.Id });

        // Every update says which version it started from, and saves nothing if another save got there first.
        builder.Property(static user => user.Version).IsConcurrencyToken();

        // A user who created, changed or archived another user cannot be deleted.
        UserReferences.Add(builder, static user => user.CreatedBy, DeleteBehavior.Restrict);
        UserReferences.Add(builder, static user => user.LastModifiedBy, DeleteBehavior.Restrict);
        UserReferences.Add(builder, static user => user.ArchivedBy, DeleteBehavior.Restrict);
    }
}
