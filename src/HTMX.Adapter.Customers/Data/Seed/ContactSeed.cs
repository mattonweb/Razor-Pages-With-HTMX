namespace HTMX.Adapter.Customers.Data.Seed;

using System;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// The 8 contacts with a part to play, written as their histories. <see cref="GeneratedContactSeed"/> adds 1,492
/// more, for 1,500 in all.
/// </summary>
/// <remarks>
/// As with <see cref="UserSeed"/>, every date is written as a time before the application started, no current row is
/// written here, every change is made by a user who was active at the time, and a contact joins an organization only
/// while it is active.
/// </remarks>
internal static class ContactSeed
{
    /// <summary>
    /// Gets Ainsley Archetype, the contact with the longest history.
    /// </summary>
    internal static ContactId AinsleyArchetype { get; } = new ContactId(10001);

    /// <summary>
    /// Gets Darcy Dataset, who is archived and named by nothing else.
    /// </summary>
    internal static ContactId DarcyDataset { get; } = new ContactId(10004);

    /// <summary>
    /// Creates every version of the 8 contacts, oldest first for each contact.
    /// </summary>
    /// <param name="startedOn">When the application started; every date is written as a time before it.</param>
    /// <returns>New instances on every call.</returns>
    internal static ContactVersion[] CreateVersions(DateTime startedOn) =>
    [

        // Ainsley Archetype: the longest history. Moved from Fictional Freight to Placeholder Logistics, so an
        // old version names an organization nobody belongs to any more; four versions in all.
        new ContactVersion
        {
            ContactId = AinsleyArchetype,
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-200),
            ChangedBy = UserSeed.DemoUser,
            Name = new DisplayName("Ainsley Archetype"),
            Email = new EmailAddress("ainsley.archetype@fictional-freight.example"),
            JobTitle = new JobTitle("Head of Samples"),
            OrganizationId = new OrganizationId(10002),
        },
        new ContactVersion
        {
            ContactId = AinsleyArchetype,
            Version = new VersionNumber(2),
            Change = ChangeKind.Edited,
            ChangedOn = startedOn.AddDays(-120),
            ChangedBy = UserSeed.SamSample,
            Name = new DisplayName("Ainsley Archetype"),
            Email = new EmailAddress("ainsley.archetype@fictional-freight.example"),
            Phone = new PhoneNumber("+1 555-0142"),
            JobTitle = new JobTitle("Head of Samples"),
            OrganizationId = new OrganizationId(10002),
        },
        new ContactVersion
        {
            ContactId = AinsleyArchetype,
            Version = new VersionNumber(3),
            Change = ChangeKind.Edited,
            ChangedOn = startedOn.AddDays(-30),
            ChangedBy = UserSeed.TaylorDummy,
            Name = new DisplayName("Ainsley Archetype"),
            Email = new EmailAddress("ainsley.archetype@placeholder.example"),
            Phone = new PhoneNumber("+1 555-0142"),
            JobTitle = new JobTitle("Head of Examples"),
            OrganizationId = OrganizationSeed.PlaceholderLogistics,
        },
        new ContactVersion
        {
            ContactId = AinsleyArchetype,
            Version = new VersionNumber(4),
            Change = ChangeKind.Edited,
            ChangedOn = startedOn.AddDays(-2),
            ChangedBy = UserSeed.PatExample,
            Name = new DisplayName("Ainsley Archetype"),
            Email = new EmailAddress("ainsley.archetype@placeholder.example"),
            Phone = new PhoneNumber("+1 555-0142"),
            JobTitle = new JobTitle("Head of Examples"),
            OrganizationId = OrganizationSeed.PlaceholderLogistics,
            Notes = new Notes("Prefers email to phone. Follow up in January about the placeholder order."),
        },

        // Bellamy Benchmark and Carmen Casefile: they belong to Mockup Media, which is archived and so cannot be
        // deleted while they do.
        new ContactVersion
        {
            ContactId = new ContactId(10002),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-200),
            ChangedBy = UserSeed.DemoUser,
            Name = new DisplayName("Bellamy Benchmark"),
            Email = new EmailAddress("bellamy.benchmark@mockup-media.example"),
            Phone = new PhoneNumber("+1 555-0117"),
            JobTitle = new JobTitle("Template Designer"),
            OrganizationId = OrganizationSeed.MockupMedia,
        },
        new ContactVersion
        {
            ContactId = new ContactId(10003),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-150),
            ChangedBy = UserSeed.TaylorDummy,
            Name = new DisplayName("Carmen Casefile"),
            Email = new EmailAddress("carmen.casefile@mockup-media.example"),
            JobTitle = new JobTitle("Draft Editor"),
            OrganizationId = OrganizationSeed.MockupMedia,
        },

        // Darcy Dataset: archived, and nothing else names them, so they can be deleted.
        new ContactVersion
        {
            ContactId = DarcyDataset,
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-180),
            ChangedBy = UserSeed.DemoUser,
            Name = new DisplayName("Darcy Dataset"),
            Email = new EmailAddress("darcy.dataset@testcase-tools.example"),
            Phone = new PhoneNumber("+1 555-0163"),
            JobTitle = new JobTitle("Test Case Planner"),
            OrganizationId = new OrganizationId(10005),
        },
        new ContactVersion
        {
            ContactId = DarcyDataset,
            Version = new VersionNumber(2),
            Change = ChangeKind.Archived,
            ChangedOn = startedOn.AddDays(-40),
            ChangedBy = UserSeed.DemoUser,
            Name = new DisplayName("Darcy Dataset"),
            Email = new EmailAddress("darcy.dataset@testcase-tools.example"),
            Phone = new PhoneNumber("+1 555-0163"),
            JobTitle = new JobTitle("Test Case Planner"),
            OrganizationId = new OrganizationId(10005),
            ArchivedOn = startedOn.AddDays(-40),
            ArchivedBy = UserSeed.DemoUser,
        },

        // Ellis Edgecase: nothing on record but a name, so every other field reads Not set.
        new ContactVersion
        {
            ContactId = new ContactId(10005),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-100),
            ChangedBy = UserSeed.DemoUser,
            Name = new DisplayName("Ellis Edgecase"),
            OrganizationId = new OrganizationId(10007),
        },

        // Fern O'Footnote: an apostrophe in a name, at an organization with an ampersand in its name.
        new ContactVersion
        {
            ContactId = new ContactId(10006),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-60),
            ChangedBy = UserSeed.DemoUser,
            Name = new DisplayName("Fern O'Footnote"),
            Email = new EmailAddress("fern.ofootnote@sample-and-sons.example"),
            JobTitle = new JobTitle("Sample Coordinator"),
            OrganizationId = new OrganizationId(10004),
        },

        // Hadley Handout: notes in three paragraphs, which the details page must keep apart.
        new ContactVersion
        {
            ContactId = new ContactId(10007),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-90),
            ChangedBy = UserSeed.PatExample,
            Name = new DisplayName("Hadley Handout"),
            Email = new EmailAddress("hadley.handout@demo-dairy.example"),
            Phone = new PhoneNumber("+1 555-0188"),
            JobTitle = new JobTitle("Chief Demonstration Officer"),
            OrganizationId = new OrganizationId(10001),
            Notes = new Notes(
                "Met at the Sample Summit, where Demo Dairy showed its placeholder cheese range.\n\n"
                + "Wants a quote for 400 crates of mock milk before the end of the quarter, delivered in two drops "
                + "to the depot on Example Street.\n\n"
                + "Call after 2 pm; mornings are for tastings."),
        },

        // Imogen Instance: created twenty minutes before the application started, so their history has one
        // entry.
        new ContactVersion
        {
            ContactId = new ContactId(10008),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddMinutes(-20),
            ChangedBy = UserSeed.DemoUser,
            Name = new DisplayName("Imogen Instance"),
            Email = new EmailAddress("imogen.instance@placeholder.example"),
            JobTitle = new JobTitle("Fixture Specialist"),
            OrganizationId = OrganizationSeed.PlaceholderLogistics,
        },
    ];
}
