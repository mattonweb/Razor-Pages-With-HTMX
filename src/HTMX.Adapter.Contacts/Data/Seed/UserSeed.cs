namespace HTMX.Adapter.Contacts.Data.Seed;

using System;
using HTMX.Adapter.Contacts.Contracts;
using HTMX.Adapter.Contacts.Entities;

/// <summary>
/// The users every run of the demo starts with, written as their histories as of <see cref="WrittenOn"/>: the 20 with
/// a part to play in the brand's specimens or in a test case. <see cref="FillerUserSeed"/> holds the 29 who fill out
/// the list, for 49 in all: 42 active and 7 archived, the counts the brand's list specimen shows.
/// </summary>
/// <remarks>
/// <para>
/// No current row is written here. <see cref="SeedWriter"/> builds each user's row from their latest version, so the
/// two always agree, exactly as a save in the running application keeps them.
/// </para>
/// <para>
/// The seed keeps the application's own rules: nobody acts before they are created or after they are archived, and
/// every user's row points only at users with smaller identifiers, so the rows can be inserted in one save.
/// </para>
/// </remarks>
internal static class UserSeed
{
    /// <summary>
    /// Gets the moment the seed was written. When the application starts, every date in the seed moves forward by the
    /// time since then, so a change written as "today" is today whenever the demo runs.
    /// </summary>
    internal static DateTime WrittenOn { get; } = new DateTime(2026, 11, 7, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Gets Demo User, who is signed in and makes most changes.
    /// </summary>
    internal static UserId DemoUser { get; } = new UserId(10001);

    /// <summary>
    /// Gets Pat Example, the user whose history is the brand's history specimen.
    /// </summary>
    internal static UserId PatExample { get; } = new UserId(10007);

    /// <summary>
    /// Gets Sam Sample, who archived Pat and was later archived.
    /// </summary>
    internal static UserId SamSample { get; } = new UserId(10009);

    /// <summary>
    /// Gets Taylor Dummy, who restored Pat and changes other users.
    /// </summary>
    internal static UserId TaylorDummy { get; } = new UserId(10011);

    /// <summary>
    /// Gets every version of the 20 users, oldest first for each user. Each call builds new instances.
    /// </summary>
    internal static UserVersion[] Versions =>
    [

        // Demo User: the signed-in user, the DU in the header. Created by itself, so every other change has
        // someone to make it.
        new UserVersion
        {
            UserId = DemoUser,
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("demo.user"),
            Email = new EmailAddress("demo@example.com"),
            DisplayName = new DisplayName("Demo User"),
        },

        // Casey Mockup: archived, and has changed nothing, so Delete permanently is allowed and asks first.
        new UserVersion
        {
            UserId = new UserId(10002),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 3, 2, 9, 30, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("casey.mockup"),
            Email = new EmailAddress("casey@example.org"),
            DisplayName = new DisplayName("Casey Mockup"),
        },
        new UserVersion
        {
            UserId = new UserId(10002),
            Version = new VersionNumber(2),
            Change = ChangeKind.Archived,
            ChangedOn = new DateTime(2026, 10, 17, 16, 40, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("casey.mockup"),
            Email = new EmailAddress("casey@example.org"),
            DisplayName = new DisplayName("Casey Mockup"),
            ArchivedOn = new DateTime(2026, 10, 17, 16, 40, 0, DateTimeKind.Utc),
            ArchivedBy = DemoUser,
        },

        // Alex, Drew, Jordan, Lee, Morgan and Robin: the rows of the brand's list specimen, with its dates.
        // Filtering on "alex" finds Alex and Alexandria.
        new UserVersion
        {
            UserId = new UserId(10003),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 11, 5, 10, 12, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("alex.placeholder"),
            Email = new EmailAddress("alex@example.org"),
            DisplayName = new DisplayName("Alex Placeholder"),
        },
        new UserVersion
        {
            UserId = new UserId(10004),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 11, 4, 15, 20, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("drew.stub"),
            Email = new EmailAddress("drew@example.org"),
            DisplayName = new DisplayName("Drew Stub"),
        },
        new UserVersion
        {
            UserId = new UserId(10005),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 11, 2, 9, 5, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("jordan.demo"),
            Email = new EmailAddress("jordan@example.net"),
            DisplayName = new DisplayName("Jordan Demo"),
        },
        new UserVersion
        {
            UserId = new UserId(10006),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 11, 1, 10, 45, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("lee.fixture"),
            Email = new EmailAddress("lee@example.net"),
            DisplayName = new DisplayName("Lee Fixture"),
        },

        // Pat Example: the brand's history specimen. Archived by Sam, restored by Taylor, display name corrected,
        // then email changed today by Demo User: five versions.
        new UserVersion
        {
            UserId = PatExample,
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 3, 12, 9, 14, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("pat.example"),
            Email = new EmailAddress("pat@placeholder.example"),
            DisplayName = new DisplayName("Pat Exampel"),
        },
        new UserVersion
        {
            UserId = PatExample,
            Version = new VersionNumber(2),
            Change = ChangeKind.Archived,
            ChangedOn = new DateTime(2026, 9, 20, 14, 2, 0, DateTimeKind.Utc),
            ChangedBy = SamSample,
            Username = new Username("pat.example"),
            Email = new EmailAddress("pat@placeholder.example"),
            DisplayName = new DisplayName("Pat Exampel"),
            ArchivedOn = new DateTime(2026, 9, 20, 14, 2, 0, DateTimeKind.Utc),
            ArchivedBy = SamSample,
        },
        new UserVersion
        {
            UserId = PatExample,
            Version = new VersionNumber(3),
            Change = ChangeKind.Restored,
            ChangedOn = new DateTime(2026, 10, 4, 10, 20, 0, DateTimeKind.Utc),
            ChangedBy = TaylorDummy,
            Username = new Username("pat.example"),
            Email = new EmailAddress("pat@placeholder.example"),
            DisplayName = new DisplayName("Pat Exampel"),
        },
        new UserVersion
        {
            UserId = PatExample,
            Version = new VersionNumber(4),
            Change = ChangeKind.Edited,
            ChangedOn = new DateTime(2026, 11, 2, 16, 5, 0, DateTimeKind.Utc),
            ChangedBy = TaylorDummy,
            Username = new Username("pat.example"),
            Email = new EmailAddress("pat@placeholder.example"),
            DisplayName = new DisplayName("Pat Example"),
        },
        new UserVersion
        {
            UserId = PatExample,
            Version = new VersionNumber(5),
            Change = ChangeKind.Edited,
            ChangedOn = new DateTime(2026, 11, 7, 8, 47, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("pat.example"),
            Email = new EmailAddress("pat@example.com"),
            DisplayName = new DisplayName("Pat Example"),
        },
        new UserVersion
        {
            UserId = new UserId(10008),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 10, 31, 13, 22, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("morgan.lorem"),
            Email = new EmailAddress("morgan@example.net"),
            DisplayName = new DisplayName("Morgan Lorem"),
        },

        // Sam Sample: archived after archiving Pat, so Pat's history names an archived user, and Delete
        // permanently is refused because another user's history points at Sam.
        new UserVersion
        {
            UserId = SamSample,
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 4, 2, 11, 0, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("sam.sample"),
            Email = new EmailAddress("sam@example.com"),
            DisplayName = new DisplayName("Sam Sample"),
        },
        new UserVersion
        {
            UserId = SamSample,
            Version = new VersionNumber(2),
            Change = ChangeKind.Archived,
            ChangedOn = new DateTime(2026, 9, 30, 15, 0, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("sam.sample"),
            Email = new EmailAddress("sam@example.com"),
            DisplayName = new DisplayName("Sam Sample"),
            ArchivedOn = new DateTime(2026, 9, 30, 15, 0, 0, DateTimeKind.Utc),
            ArchivedBy = DemoUser,
        },
        new UserVersion
        {
            UserId = new UserId(10010),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 10, 24, 11, 30, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("robin.testcase"),
            Email = new EmailAddress("robin@example.com"),
            DisplayName = new DisplayName("Robin Testcase"),
        },

        // Taylor Dummy: changes other users, and was last changed yesterday by Pat rather than by Demo User.
        new UserVersion
        {
            UserId = TaylorDummy,
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 5, 4, 9, 40, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("taylor.dummy"),
            Email = new EmailAddress("taylor@dummy.example"),
            DisplayName = new DisplayName("Taylor Dummy"),
        },
        new UserVersion
        {
            UserId = TaylorDummy,
            Version = new VersionNumber(2),
            Change = ChangeKind.Edited,
            ChangedOn = new DateTime(2026, 11, 6, 17, 15, 0, DateTimeKind.Utc),
            ChangedBy = PatExample,
            Username = new Username("taylor.dummy"),
            Email = new EmailAddress("taylor@example.com"),
            DisplayName = new DisplayName("Taylor Dummy"),
        },

        // Jamie O'Placeholder: an apostrophe in a name, which the pages must encode in text and attributes.
        new UserVersion
        {
            UserId = new UserId(10012),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 6, 10, 14, 25, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("jamie.oplaceholder"),
            Email = new EmailAddress("jamie@example.net"),
            DisplayName = new DisplayName("Jamie O'Placeholder"),
        },

        // Renée Exemple: an accented letter, which NOCASE does not fold: filtering on "RENÉE" finds no one.
        new UserVersion
        {
            UserId = new UserId(10013),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 6, 18, 10, 5, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("renee.exemple"),
            Email = new EmailAddress("renee@example.org"),
            DisplayName = new DisplayName("Renée Exemple"),
        },

        // Alexandria Wireframe-Boilerplate: long values, which must wrap in the grid and the side panel.
        new UserVersion
        {
            UserId = new UserId(10014),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 7, 1, 13, 0, 0, DateTimeKind.Utc),
            ChangedBy = TaylorDummy,
            Username = new Username("alexandria.wireframe-boilerplate"),
            Email = new EmailAddress("alexandria.wireframe-boilerplate@subdomain.example.org"),
            DisplayName = new DisplayName("Alexandria Wireframe-Boilerplate"),
        },

        // Kit Sandbox: created half an hour before the seed was written, so its history has one entry.
        new UserVersion
        {
            UserId = new UserId(10015),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 11, 7, 8, 30, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("kit.sandbox"),
            Email = new EmailAddress("kit@example.com"),
            DisplayName = new DisplayName("Kit Sandbox"),
        },

        // Hollis, Logan, Kim, Val and Ari: the rest of the seven archived users. Kim and Ari are the brand's
        // Archived specimen, with its dates.
        new UserVersion
        {
            UserId = new UserId(10016),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("hollis.dolor"),
            Email = new EmailAddress("hollis@example.org"),
            DisplayName = new DisplayName("Hollis Dolor"),
        },
        new UserVersion
        {
            UserId = new UserId(10016),
            Version = new VersionNumber(2),
            Change = ChangeKind.Archived,
            ChangedOn = new DateTime(2026, 8, 14, 11, 10, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("hollis.dolor"),
            Email = new EmailAddress("hollis@example.org"),
            DisplayName = new DisplayName("Hollis Dolor"),
            ArchivedOn = new DateTime(2026, 8, 14, 11, 10, 0, DateTimeKind.Utc),
            ArchivedBy = DemoUser,
        },
        new UserVersion
        {
            UserId = new UserId(10017),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 4, 8, 15, 45, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("logan.faux"),
            Email = new EmailAddress("logan@example.net"),
            DisplayName = new DisplayName("Logan Faux"),
        },
        new UserVersion
        {
            UserId = new UserId(10017),
            Version = new VersionNumber(2),
            Change = ChangeKind.Archived,
            ChangedOn = new DateTime(2026, 7, 22, 9, 30, 0, DateTimeKind.Utc),
            ChangedBy = TaylorDummy,
            Username = new Username("logan.faux"),
            Email = new EmailAddress("logan@example.net"),
            DisplayName = new DisplayName("Logan Faux"),
            ArchivedOn = new DateTime(2026, 7, 22, 9, 30, 0, DateTimeKind.Utc),
            ArchivedBy = TaylorDummy,
        },
        new UserVersion
        {
            UserId = new UserId(10018),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 4, 20, 13, 15, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("kim.prototype"),
            Email = new EmailAddress("kim@example.org"),
            DisplayName = new DisplayName("Kim Prototype"),
        },
        new UserVersion
        {
            UserId = new UserId(10018),
            Version = new VersionNumber(2),
            Change = ChangeKind.Archived,
            ChangedOn = new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("kim.prototype"),
            Email = new EmailAddress("kim@example.org"),
            DisplayName = new DisplayName("Kim Prototype"),
            ArchivedOn = new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc),
            ArchivedBy = DemoUser,
        },
        new UserVersion
        {
            UserId = new UserId(10019),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 5, 19, 16, 20, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("val.vanilla"),
            Email = new EmailAddress("val@example.com"),
            DisplayName = new DisplayName("Val Vanilla"),
        },
        new UserVersion
        {
            UserId = new UserId(10019),
            Version = new VersionNumber(2),
            Change = ChangeKind.Archived,
            ChangedOn = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc),
            ChangedBy = PatExample,
            Username = new Username("val.vanilla"),
            Email = new EmailAddress("val@example.com"),
            DisplayName = new DisplayName("Val Vanilla"),
            ArchivedOn = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc),
            ArchivedBy = PatExample,
        },
        new UserVersion
        {
            UserId = new UserId(10021),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 5, 11, 10, 30, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("ari.template"),
            Email = new EmailAddress("ari@example.net"),
            DisplayName = new DisplayName("Ari Template"),
        },
        new UserVersion
        {
            UserId = new UserId(10021),
            Version = new VersionNumber(2),
            Change = ChangeKind.Archived,
            ChangedOn = new DateTime(2026, 10, 2, 14, 0, 0, DateTimeKind.Utc),
            ChangedBy = DemoUser,
            Username = new Username("ari.template"),
            Email = new EmailAddress("ari@example.net"),
            DisplayName = new DisplayName("Ari Template"),
            ArchivedOn = new DateTime(2026, 10, 2, 14, 0, 0, DateTimeKind.Utc),
            ArchivedBy = DemoUser,
        },
    ];
}
