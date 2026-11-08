namespace HTMX.Adapter.Customers.Data.Seed;

using System;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// The users every run of the demo starts with, written as their histories: the 20 with a part to play in a test
/// case. <see cref="FillerUserSeed"/> holds the 29 who fill out the list, for 49 in all: 42 active and 7 archived, so
/// the list runs to five pages of ten with a last page of nine.
/// </summary>
/// <remarks>
/// <para>
/// Every date is written as a time before the application started, so the data is as recent whenever the demo runs:
/// Pat Example was edited minutes ago, and the oldest user was created about eight months ago.
/// </para>
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
    /// Gets Demo User, who is signed in and makes most changes.
    /// </summary>
    internal static UserId DemoUser { get; } = new UserId(10001);

    /// <summary>
    /// Gets Pat Example, the user with the longest history.
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
    /// Creates every version of the 20 users, oldest first for each user.
    /// </summary>
    /// <param name="startedOn">When the application started; every date is written as a time before it.</param>
    /// <returns>New instances on every call.</returns>
    internal static UserVersion[] CreateVersions(DateTime startedOn) =>
    [

        // Demo User: the signed-in user, the DU in the header. Created by itself, so every other change has
        // someone to make it.
        new UserVersion
        {
            UserId = DemoUser,
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-251),
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
            ChangedOn = startedOn.AddDays(-250),
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
            ChangedOn = startedOn.AddDays(-21),
            ChangedBy = DemoUser,
            Username = new Username("casey.mockup"),
            Email = new EmailAddress("casey@example.org"),
            DisplayName = new DisplayName("Casey Mockup"),
            ArchivedOn = startedOn.AddDays(-21),
            ArchivedBy = DemoUser,
        },

        // Alex, Drew, Jordan, Lee, Morgan and Robin: created between two days and two weeks ago, so the relative
        // dates on a list read "2 days ago" through "2 weeks ago". Filtering on "alex" finds Alex and Alexandria.
        new UserVersion
        {
            UserId = new UserId(10003),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-2),
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
            ChangedOn = startedOn.AddDays(-3),
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
            ChangedOn = startedOn.AddDays(-5),
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
            ChangedOn = startedOn.AddDays(-6),
            ChangedBy = DemoUser,
            Username = new Username("lee.fixture"),
            Email = new EmailAddress("lee@example.net"),
            DisplayName = new DisplayName("Lee Fixture"),
        },

        // Pat Example: the longest history. Archived by Sam, restored by Taylor, display name corrected, then email
        // changed today by Demo User: five versions, enough for the timeline, the diff and a roll-back.
        new UserVersion
        {
            UserId = PatExample,
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-240),
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
            ChangedOn = startedOn.AddDays(-48),
            ChangedBy = SamSample,
            Username = new Username("pat.example"),
            Email = new EmailAddress("pat@placeholder.example"),
            DisplayName = new DisplayName("Pat Exampel"),
            ArchivedOn = startedOn.AddDays(-48),
            ArchivedBy = SamSample,
        },
        new UserVersion
        {
            UserId = PatExample,
            Version = new VersionNumber(3),
            Change = ChangeKind.Restored,
            ChangedOn = startedOn.AddDays(-34),
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
            ChangedOn = startedOn.AddDays(-5),
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
            ChangedOn = startedOn.AddMinutes(-13),
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
            ChangedOn = startedOn.AddDays(-7),
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
            ChangedOn = startedOn.AddDays(-219),
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
            ChangedOn = startedOn.AddDays(-38),
            ChangedBy = DemoUser,
            Username = new Username("sam.sample"),
            Email = new EmailAddress("sam@example.com"),
            DisplayName = new DisplayName("Sam Sample"),
            ArchivedOn = startedOn.AddDays(-38),
            ArchivedBy = DemoUser,
        },
        new UserVersion
        {
            UserId = new UserId(10010),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-14),
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
            ChangedOn = startedOn.AddDays(-187),
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
            ChangedOn = startedOn.AddDays(-1),
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
            ChangedOn = startedOn.AddDays(-150),
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
            ChangedOn = startedOn.AddDays(-142),
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
            ChangedOn = startedOn.AddDays(-129),
            ChangedBy = TaylorDummy,
            Username = new Username("alexandria.wireframe-boilerplate"),
            Email = new EmailAddress("alexandria.wireframe-boilerplate@subdomain.example.org"),
            DisplayName = new DisplayName("Alexandria Wireframe-Boilerplate"),
        },

        // Kit Sandbox: created half an hour before the application started, so its history has one entry.
        new UserVersion
        {
            UserId = new UserId(10015),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddMinutes(-30),
            ChangedBy = DemoUser,
            Username = new Username("kit.sandbox"),
            Email = new EmailAddress("kit@example.com"),
            DisplayName = new DisplayName("Kit Sandbox"),
        },

        // Hollis, Logan, Kim, Val and Ari: the rest of the seven archived users, archived by three different users
        // between three months and ten days ago.
        new UserVersion
        {
            UserId = new UserId(10016),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-232),
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
            ChangedOn = startedOn.AddDays(-85),
            ChangedBy = DemoUser,
            Username = new Username("hollis.dolor"),
            Email = new EmailAddress("hollis@example.org"),
            DisplayName = new DisplayName("Hollis Dolor"),
            ArchivedOn = startedOn.AddDays(-85),
            ArchivedBy = DemoUser,
        },
        new UserVersion
        {
            UserId = new UserId(10017),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-213),
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
            ChangedOn = startedOn.AddDays(-108),
            ChangedBy = TaylorDummy,
            Username = new Username("logan.faux"),
            Email = new EmailAddress("logan@example.net"),
            DisplayName = new DisplayName("Logan Faux"),
            ArchivedOn = startedOn.AddDays(-108),
            ArchivedBy = TaylorDummy,
        },
        new UserVersion
        {
            UserId = new UserId(10018),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-201),
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
            ChangedOn = startedOn.AddDays(-56),
            ChangedBy = DemoUser,
            Username = new Username("kim.prototype"),
            Email = new EmailAddress("kim@example.org"),
            DisplayName = new DisplayName("Kim Prototype"),
            ArchivedOn = startedOn.AddDays(-56),
            ArchivedBy = DemoUser,
        },
        new UserVersion
        {
            UserId = new UserId(10019),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-172),
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
            ChangedOn = startedOn.AddDays(-10),
            ChangedBy = PatExample,
            Username = new Username("val.vanilla"),
            Email = new EmailAddress("val@example.com"),
            DisplayName = new DisplayName("Val Vanilla"),
            ArchivedOn = startedOn.AddDays(-10),
            ArchivedBy = PatExample,
        },
        new UserVersion
        {
            UserId = new UserId(10021),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn.AddDays(-180),
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
            ChangedOn = startedOn.AddDays(-36),
            ChangedBy = DemoUser,
            Username = new Username("ari.template"),
            Email = new EmailAddress("ari@example.net"),
            DisplayName = new DisplayName("Ari Template"),
            ArchivedOn = startedOn.AddDays(-36),
            ArchivedBy = DemoUser,
        },
    ];
}
