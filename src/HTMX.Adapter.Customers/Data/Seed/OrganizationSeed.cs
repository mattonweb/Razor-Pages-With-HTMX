namespace HTMX.Adapter.Customers.Data.Seed;

using System;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// The 15 organizations with a part to play, written as their histories: 13 active and 2 archived.
/// <see cref="GeneratedOrganizationSeed"/> adds 985 more, for 1,000 in all.
/// </summary>
/// <remarks>
/// As with <see cref="UserSeed"/>, every date is written as a time before the application started, no current row is
/// written here, and every change is made by a user who existed and was active at the time.
/// </remarks>
internal static class OrganizationSeed
{
    /// <summary>
    /// Gets Placeholder Logistics, the organization whose history has two versions.
    /// </summary>
    internal static OrganizationId PlaceholderLogistics { get; } = new OrganizationId(10003);

    /// <summary>
    /// Gets Mockup Media, which is archived.
    /// </summary>
    internal static OrganizationId MockupMedia { get; } = new OrganizationId(10009);

    /// <summary>
    /// Gets Draft Distribution, which is archived and has nothing belonging to it.
    /// </summary>
    internal static OrganizationId DraftDistribution { get; } = new OrganizationId(10013);

    /// <summary>
    /// Creates every version of the 15 organizations, oldest first for each organization.
    /// </summary>
    /// <param name="startedOn">When the application started; every date is written as a time before it.</param>
    /// <returns>New instances on every call.</returns>
    internal static OrganizationVersion[] CreateVersions(DateTime startedOn) =>
    [
        new OrganizationVersion
        {
            OrganizationId = new OrganizationId(10001),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-245),
            ChangedBy = UserSeed.DemoUser,
            Name = new OrganizationName("Demo Dairy Co."),
            Industry = Industry.FoodAndDrink,
            Website = new WebAddress("https://demo-dairy.example"),
        },

        // Fictional Freight: Ainsley Archetype's organization before Placeholder Logistics. No contact belongs to it
        // now, so it can be deleted, although Ainsley's history still names it.
        new OrganizationVersion
        {
            OrganizationId = new OrganizationId(10002),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-238),
            ChangedBy = UserSeed.DemoUser,
            Name = new OrganizationName("Fictional Freight"),
            Industry = Industry.Shipping,
            Website = new WebAddress("https://fictional-freight.example"),
        },

        // Placeholder Logistics: its website changed, so its history has two versions. It has 18 contacts, so its
        // own list of contacts pages.
        new OrganizationVersion
        {
            OrganizationId = PlaceholderLogistics,
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-244),
            ChangedBy = UserSeed.DemoUser,
            Name = new OrganizationName("Placeholder Logistics"),
            Industry = Industry.Shipping,
            Website = new WebAddress("https://placeholder-logistics.example"),
        },
        new OrganizationVersion
        {
            OrganizationId = PlaceholderLogistics,
            Version = new VersionNumber(2),
            Change = ChangeKind.Edited,
            ChangedOn = startedOn.AddDays(-61),
            ChangedBy = UserSeed.TaylorDummy,
            Name = new OrganizationName("Placeholder Logistics"),
            Industry = Industry.Shipping,
            Website = new WebAddress("https://placeholder.example"),
        },

        // Sample & Sons: an ampersand, which the pages must encode, created by Sam, who is now archived.
        new OrganizationVersion
        {
            OrganizationId = new OrganizationId(10004),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-120),
            ChangedBy = UserSeed.SamSample,
            Name = new OrganizationName("Sample & Sons"),
            Industry = Industry.Retail,
            Website = new WebAddress("https://sample-and-sons.example"),
        },
        new OrganizationVersion
        {
            OrganizationId = new OrganizationId(10005),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-232),
            ChangedBy = UserSeed.DemoUser,
            Name = new OrganizationName("Testcase Tools"),
            Industry = Industry.Manufacturing,
            Website = new WebAddress("https://testcase-tools.example"),
        },

        // From here on, an organization with no comment was created once and never changed.
        new OrganizationVersion
        {
            OrganizationId = new OrganizationId(10006),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-175),
            ChangedBy = UserSeed.TaylorDummy,
            Name = new OrganizationName("Dummy Data Ltd."),
            Industry = Industry.Technology,
            Website = new WebAddress("https://dummy-data.example"),
        },

        // Fixture Foods: no website, so its details page shows Not set.
        new OrganizationVersion
        {
            OrganizationId = new OrganizationId(10007),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-160),
            ChangedBy = UserSeed.DemoUser,
            Name = new OrganizationName("Fixture Foods"),
            Industry = Industry.FoodAndDrink,
        },
        new OrganizationVersion
        {
            OrganizationId = new OrganizationId(10008),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-141),
            ChangedBy = UserSeed.PatExample,
            Name = new OrganizationName("Lorem Ipsum Labs"),
            Industry = Industry.Technology,
            Website = new WebAddress("https://lorem-ipsum-labs.example"),
        },

        // Mockup Media: archived, with two contacts, so it cannot be deleted while they belong to it.
        new OrganizationVersion
        {
            OrganizationId = MockupMedia,
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-226),
            ChangedBy = UserSeed.DemoUser,
            Name = new OrganizationName("Mockup Media"),
            Industry = Industry.Media,
            Website = new WebAddress("https://mockup-media.example"),
        },
        new OrganizationVersion
        {
            OrganizationId = MockupMedia,
            Version = new VersionNumber(2),
            Change = ChangeKind.Archived,
            ChangedOn = startedOn.AddDays(-22),
            ChangedBy = UserSeed.DemoUser,
            Name = new OrganizationName("Mockup Media"),
            Industry = Industry.Media,
            Website = new WebAddress("https://mockup-media.example"),
            ArchivedOn = startedOn.AddDays(-22),
            ArchivedBy = UserSeed.DemoUser,
        },
        new OrganizationVersion
        {
            OrganizationId = new OrganizationId(10010),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-133),
            ChangedBy = UserSeed.TaylorDummy,
            Name = new OrganizationName("Prototype Paper"),
            Industry = Industry.Manufacturing,
            Website = new WebAddress("https://prototype-paper.example"),
        },
        new OrganizationVersion
        {
            OrganizationId = new OrganizationId(10011),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-97),
            ChangedBy = UserSeed.DemoUser,
            Name = new OrganizationName("Stub Supply Co."),
            Industry = Industry.Retail,
            Website = new WebAddress("https://stub-supply.example"),
        },
        new OrganizationVersion
        {
            OrganizationId = new OrganizationId(10012),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-84),
            ChangedBy = UserSeed.TaylorDummy,
            Name = new OrganizationName("Template Textiles"),
            Industry = Industry.Manufacturing,
            Website = new WebAddress("https://template-textiles.example"),
        },

        // Draft Distribution: archived, and no contact belongs to it, so it can be deleted.
        new OrganizationVersion
        {
            OrganizationId = DraftDistribution,
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-150),
            ChangedBy = UserSeed.TaylorDummy,
            Name = new OrganizationName("Draft Distribution"),
            Industry = Industry.Shipping,
            Website = new WebAddress("https://draft-distribution.example"),
        },
        new OrganizationVersion
        {
            OrganizationId = DraftDistribution,
            Version = new VersionNumber(2),
            Change = ChangeKind.Archived,
            ChangedOn = startedOn.AddDays(-90),
            ChangedBy = UserSeed.DemoUser,
            Name = new OrganizationName("Draft Distribution"),
            Industry = Industry.Shipping,
            Website = new WebAddress("https://draft-distribution.example"),
            ArchivedOn = startedOn.AddDays(-90),
            ArchivedBy = UserSeed.DemoUser,
        },

        // Sandbox Software: created by Jamie O'Placeholder, whose only change it is, so an organization alone
        // keeps a user from being deleted.
        new OrganizationVersion
        {
            OrganizationId = new OrganizationId(10014),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-30),
            ChangedBy = new UserId(10012),
            Name = new OrganizationName("Sandbox Software"),
            Industry = Industry.Technology,
            Website = new WebAddress("https://sandbox-software.example"),
        },
        new OrganizationVersion
        {
            OrganizationId = new OrganizationId(10015),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-12),
            ChangedBy = UserSeed.DemoUser,
            Name = new OrganizationName("Specimen Studios"),
            Industry = Industry.Media,
            Website = new WebAddress("https://specimen-studios.example"),
        },
    ];
}
