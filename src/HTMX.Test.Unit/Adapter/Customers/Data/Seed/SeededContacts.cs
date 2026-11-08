namespace HTMX.Test.Unit.Adapter.Customers.Data.Seed;

using HTMX.Adapter.Customers.Entities;

/// <summary>
/// What <see cref="ContactSeedTests"/> reads back from a seeded database: the contacts, and the versions their rules
/// are checked against.
/// </summary>
internal sealed record SeededContacts
{
    /// <summary>
    /// Gets every contact's current row.
    /// </summary>
    required public Contact[] Contacts { get; init; }

    /// <summary>
    /// Gets every version of every contact.
    /// </summary>
    required public ContactVersion[] Versions { get; init; }

    /// <summary>
    /// Gets every version of every organization, which says when each was active.
    /// </summary>
    required public OrganizationVersion[] OrganizationVersions { get; init; }

    /// <summary>
    /// Gets every version of every user, which says when each was active.
    /// </summary>
    required public UserVersion[] UserVersions { get; init; }
}
