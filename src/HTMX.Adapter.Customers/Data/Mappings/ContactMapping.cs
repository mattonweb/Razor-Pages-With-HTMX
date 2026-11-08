namespace HTMX.Adapter.Customers.Data.Mappings;

using System;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Maps <see cref="Contact"/> to the Contacts table.
/// </summary>
internal sealed class ContactMapping : IEntityTypeConfiguration<Contact>
{
    /// <summary>
    /// Names the table, sorts names however they are capitalized, makes the version number refuse a save from an
    /// out-of-date form, ties each contact to its organization, and points the three "by" columns at users.
    /// </summary>
    /// <param name="builder">The builder for the contact entity.</param>
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Contacts");
        builder.HasKey(static contact => contact.Id);

        // AUTOINCREMENT, as for users: a deleted contact's identifier is never given out again.
        builder.Property(static contact => contact.Id).UseAutoincrement();

        // Names are not unique, since two people can share one, but they sort and filter however they are capitalized.
        builder.Property(static contact => contact.Name).UseCollation("NOCASE");

        builder.Property(static contact => contact.Version).IsConcurrencyToken();

        // Every contact belongs to an organization, which cannot be deleted while a contact belongs to it.
        ReferenceCollectionBuilder<Organization, Contact> organization = builder.HasOne<Organization>().WithMany();
        organization.HasForeignKey(static contact => contact.OrganizationId);
        organization.OnDelete(DeleteBehavior.Restrict);

        // A user who created, changed or archived a contact cannot be deleted.
        UserReferences.Add(builder, static contact => contact.CreatedBy, DeleteBehavior.Restrict);
        UserReferences.Add(builder, static contact => contact.LastModifiedBy, DeleteBehavior.Restrict);
        UserReferences.Add(builder, static contact => contact.ArchivedBy, DeleteBehavior.Restrict);
    }
}
