namespace HTMX.Adapter.Customers.Mappers;

using System;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Maps <see cref="Organization"/> to the Organizations table.
/// </summary>
internal sealed class OrganizationMapper : IEntityTypeConfiguration<Organization>
{
    /// <summary>
    /// Names the table, makes names unique however they are capitalized, indexes the columns the list sorts by, makes
    /// the version number refuse a save from an out-of-date form, and points the three "by" columns at users.
    /// </summary>
    /// <param name="builder">The builder for the organization entity.</param>
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Organizations");
        builder.HasKey(static organization => organization.Id);

        // AUTOINCREMENT, as for users: a deleted organization's identifier is never given out again.
        builder.Property(static organization => organization.Id).UseAutoincrement();

        // NOCASE, as for usernames: Placeholder Logistics and PLACEHOLDER LOGISTICS are the same organization.
        builder.Property(static organization => organization.Name).UseCollation("NOCASE");
        builder.HasIndex(static organization => organization.Name).IsUnique();

        // Key-based paging seeks to a value in the sorted column and then settles ties on the identifier, so each
        // sortable column has an index on the pair. Name needs no second index: names never tie, so its unique index
        // already gives the whole order. Contacts is a count, not a column, and no index can hold it.
        builder.HasIndex(static organization => new { organization.Industry, organization.Id });
        builder.HasIndex(static organization => new { organization.LastModifiedOn, organization.Id });

        builder.Property(static organization => organization.Version).IsConcurrencyToken();

        // A user who created, changed or archived an organization cannot be deleted.
        UserReferences.Add(builder, static organization => organization.CreatedBy, DeleteBehavior.Restrict);
        UserReferences.Add(builder, static organization => organization.LastModifiedBy, DeleteBehavior.Restrict);
        UserReferences.Add(builder, static organization => organization.ArchivedBy, DeleteBehavior.Restrict);
    }
}
