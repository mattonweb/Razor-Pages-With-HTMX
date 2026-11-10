namespace HTMX.Adapter.Customers.Data;

using System;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data.Conversions;
using HTMX.Adapter.Customers.Data.Mappings;
using HTMX.Adapter.Customers.Extensions;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The customer database's Entity Framework context. One instance serves one request and is then discarded, so nothing
/// it has read outlives the request that read it.
/// </summary>
/// <remarks>
/// Tables are reached with <c>Set&lt;User&gt;()</c> rather than a property per table, so the context has no public
/// state; each mapping names its table instead.
/// </remarks>
internal sealed class CustomersDatabaseContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomersDatabaseContext"/> class.
    /// </summary>
    /// <param name="options">The options that point the context at its database.</param>
    public CustomersDatabaseContext(DbContextOptions<CustomersDatabaseContext> options)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(options);
    }

    /// <summary>
    /// Says, once for every entity, how each wrapped value, each choice from a list and every date is stored.
    /// </summary>
    /// <param name="configurationBuilder">The builder for the model's conventions.</param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        configurationBuilder.Properties<UserId>().HaveConversion<UserIdConverter>();
        configurationBuilder.Properties<Username>().HaveConversion<UsernameConverter>();
        configurationBuilder.Properties<EmailAddress>().HaveConversion<EmailAddressConverter>();
        configurationBuilder.Properties<DisplayName>().HaveConversion<DisplayNameConverter>();
        configurationBuilder.Properties<OrganizationId>().HaveConversion<OrganizationIdConverter>();
        configurationBuilder.Properties<OrganizationName>().HaveConversion<OrganizationNameConverter>();
        configurationBuilder.Properties<WebAddress>().HaveConversion<WebAddressConverter>();
        configurationBuilder.Properties<ContactId>().HaveConversion<ContactIdConverter>();
        configurationBuilder.Properties<PhoneNumber>().HaveConversion<PhoneNumberConverter>();
        configurationBuilder.Properties<JobTitle>().HaveConversion<JobTitleConverter>();
        configurationBuilder.Properties<Notes>().HaveConversion<NotesConverter>();
        configurationBuilder.Properties<VersionNumber>().HaveConversion<VersionNumberConverter>();
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();

        // Choices from a list are stored by name, so the SQL log reads Edited and Shipping rather than 1 and 4.
        configurationBuilder.Properties<ChangeKind>().HaveConversion<string>();
        configurationBuilder.Properties<Industry>().HaveConversion<string>();

        // A value that a query hands to one of ComparisonTranslations' comparisons is a parameter, not a column, so it
        // needs its own mapping, the same as its column's.
        configurationBuilder.DefaultTypeMapping<UserId>().HasConversion<UserIdConverter>();
        configurationBuilder.DefaultTypeMapping<Username>().HasConversion<UsernameConverter>();
        configurationBuilder.DefaultTypeMapping<EmailAddress>().HasConversion<EmailAddressConverter>();
        configurationBuilder.DefaultTypeMapping<DisplayName>().HasConversion<DisplayNameConverter>();
        configurationBuilder.DefaultTypeMapping<OrganizationId>().HasConversion<OrganizationIdConverter>();
        configurationBuilder.DefaultTypeMapping<OrganizationName>().HasConversion<OrganizationNameConverter>();
        configurationBuilder.DefaultTypeMapping<ContactId>().HasConversion<ContactIdConverter>();
    }

    /// <summary>
    /// Maps each entity to its table.
    /// </summary>
    /// <param name="modelBuilder">The builder for the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfiguration(new UserMapping());
        modelBuilder.ApplyConfiguration(new UserVersionMapping());
        modelBuilder.ApplyConfiguration(new OrganizationMapping());
        modelBuilder.ApplyConfiguration(new OrganizationVersionMapping());
        modelBuilder.ApplyConfiguration(new ContactMapping());
        modelBuilder.ApplyConfiguration(new ContactVersionMapping());

        // The comparisons key-based paging seeks with, each turned into SQL on the bare column.
        ComparisonTranslations.Map(modelBuilder, typeof(UserIdExtensions));
        ComparisonTranslations.Map(modelBuilder, typeof(UsernameExtensions));
        ComparisonTranslations.Map(modelBuilder, typeof(EmailAddressExtensions));
        ComparisonTranslations.Map(modelBuilder, typeof(DisplayNameExtensions));
        ComparisonTranslations.Map(modelBuilder, typeof(OrganizationIdExtensions));
        ComparisonTranslations.Map(modelBuilder, typeof(OrganizationNameExtensions));
        ComparisonTranslations.Map(modelBuilder, typeof(ContactIdExtensions));
    }
}
