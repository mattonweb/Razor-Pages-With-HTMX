namespace HTMX.Adapter.Customers.Data.Mappings;

using System;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Maps <see cref="OrganizationVersion"/> to the OrganizationVersions table.
/// </summary>
internal sealed class OrganizationVersionMapping : IEntityTypeConfiguration<OrganizationVersion>
{
    /// <summary>
    /// Names the table, keys each version by its organization and number, and points the version at its organization
    /// and at whoever made the change.
    /// </summary>
    /// <param name="builder">The builder for the organization version entity.</param>
    public void Configure(EntityTypeBuilder<OrganizationVersion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("OrganizationVersions");
        builder.HasKey(static version => new { version.OrganizationId, version.Version });

        // An organization's history goes with it when it is deleted.
        ReferenceCollectionBuilder<Organization, OrganizationVersion> organization =
            builder.HasOne<Organization>().WithMany();
        organization.HasForeignKey(static version => version.OrganizationId);
        organization.OnDelete(DeleteBehavior.Cascade);

        // A user who made a change in an organization's history cannot be deleted.
        UserReferences.Add(builder, static version => version.ChangedBy, DeleteBehavior.Restrict);
        UserReferences.Add(builder, static version => version.ArchivedBy, DeleteBehavior.Restrict);
    }
}
