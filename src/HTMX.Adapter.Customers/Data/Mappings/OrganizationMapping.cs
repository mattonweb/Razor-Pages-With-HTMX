namespace HTMX.Adapter.Customers.Data.Mappings;

using System;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Maps <see cref="Organization"/> to the Organizations table.
/// </summary>
internal sealed class OrganizationMapping : IEntityTypeConfiguration<Organization>
{
    /// <summary>
    /// Names the table, makes names unique however they are capitalized, makes the version number refuse a save from
    /// an out-of-date form, and points the three "by" columns at users.
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

        builder.Property(static organization => organization.Version).IsConcurrencyToken();

        // A user who created, changed or archived an organization cannot be deleted.
        UserReferences.Add(builder, static organization => organization.CreatedBy, DeleteBehavior.Restrict);
        UserReferences.Add(builder, static organization => organization.LastModifiedBy, DeleteBehavior.Restrict);
        UserReferences.Add(builder, static organization => organization.ArchivedBy, DeleteBehavior.Restrict);
    }
}
