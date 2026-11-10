namespace HTMX.Adapter.Customers.Mappers;

using System;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Maps <see cref="ContactVersion"/> to the ContactVersions table.
/// </summary>
internal sealed class ContactVersionMapper : IEntityTypeConfiguration<ContactVersion>
{
    /// <summary>
    /// Names the table, keys each version by its contact and number, and points the version at its contact and at
    /// whoever made the change.
    /// </summary>
    /// <param name="builder">The builder for the contact version entity.</param>
    public void Configure(EntityTypeBuilder<ContactVersion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ContactVersions");
        builder.HasKey(static version => new { version.ContactId, version.Version });

        // A contact's history goes with them when they are deleted.
        ReferenceCollectionBuilder<Contact, ContactVersion> contact = builder.HasOne<Contact>().WithMany();
        contact.HasForeignKey(static version => version.ContactId);
        contact.OnDelete(DeleteBehavior.Cascade);

        // OrganizationId is deliberately not a foreign key: a version records the organization the contact belonged to
        // then, and an organization nobody belongs to any more can be deleted even though old versions name it.
        UserReferences.Add(builder, static version => version.ChangedBy, DeleteBehavior.Restrict);
        UserReferences.Add(builder, static version => version.ArchivedBy, DeleteBehavior.Restrict);
    }
}
