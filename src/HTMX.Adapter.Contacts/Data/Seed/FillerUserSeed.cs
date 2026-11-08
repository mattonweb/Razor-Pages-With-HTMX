namespace HTMX.Adapter.Contacts.Data.Seed;

using System;
using HTMX.Adapter.Contacts.Contracts;
using HTMX.Adapter.Contacts.Entities;

/// <summary>
/// The 29 seed users who fill out the list, so it runs to five pages of ten. Each was created once, by Demo User or by
/// Taylor Dummy, and never changed. <see cref="UserSeed"/> holds the 20 with a part to play.
/// </summary>
internal static class FillerUserSeed
{
    /// <summary>
    /// Gets the one version of each filler user. Each call builds new instances.
    /// </summary>
    internal static UserVersion[] Versions =>
    [
        new UserVersion
        {
            UserId = new UserId(10020),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 3, 16, 9, 0, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("avery.draft"),
            Email = new EmailAddress("avery@example.com"),
            DisplayName = new DisplayName("Avery Draft"),
        },
        new UserVersion
        {
            UserId = new UserId(10022),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 3, 18, 10, 20, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("bailey.blueprint"),
            Email = new EmailAddress("bailey@example.org"),
            DisplayName = new DisplayName("Bailey Blueprint"),
        },
        new UserVersion
        {
            UserId = new UserId(10023),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 3, 23, 14, 5, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("blair.sketch"),
            Email = new EmailAddress("blair@example.net"),
            DisplayName = new DisplayName("Blair Sketch"),
        },
        new UserVersion
        {
            UserId = new UserId(10024),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 3, 27, 11, 40, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("cameron.scaffold"),
            Email = new EmailAddress("cameron@example.com"),
            DisplayName = new DisplayName("Cameron Scaffold"),
        },
        new UserVersion
        {
            UserId = new UserId(10025),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 4, 1, 9, 15, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("charlie.specimen"),
            Email = new EmailAddress("charlie@example.org"),
            DisplayName = new DisplayName("Charlie Specimen"),
        },
        new UserVersion
        {
            UserId = new UserId(10026),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 4, 6, 16, 30, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("dakota.dryrun"),
            Email = new EmailAddress("dakota@example.net"),
            DisplayName = new DisplayName("Dakota Dryrun"),
        },
        new UserVersion
        {
            UserId = new UserId(10027),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 4, 13, 10, 50, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("devon.mock"),
            Email = new EmailAddress("devon@example.com"),
            DisplayName = new DisplayName("Devon Mock"),
        },
        new UserVersion
        {
            UserId = new UserId(10028),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 4, 17, 13, 35, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("eden.filler"),
            Email = new EmailAddress("eden@example.org"),
            DisplayName = new DisplayName("Eden Filler"),
        },
        new UserVersion
        {
            UserId = new UserId(10029),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 4, 24, 9, 5, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("emery.trial"),
            Email = new EmailAddress("emery@example.net"),
            DisplayName = new DisplayName("Emery Trial"),
        },
        new UserVersion
        {
            UserId = new UserId(10030),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 4, 29, 15, 10, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("finley.pilot"),
            Email = new EmailAddress("finley@example.com"),
            DisplayName = new DisplayName("Finley Pilot"),
        },
        new UserVersion
        {
            UserId = new UserId(10031),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 5, 6, 11, 25, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.TaylorDummy,
            Username = new Username("frankie.proxy"),
            Email = new EmailAddress("frankie@example.org"),
            DisplayName = new DisplayName("Frankie Proxy"),
        },
        new UserVersion
        {
            UserId = new UserId(10032),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 5, 13, 14, 45, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.TaylorDummy,
            Username = new Username("gray.decoy"),
            Email = new EmailAddress("gray@example.net"),
            DisplayName = new DisplayName("Gray Decoy"),
        },
        new UserVersion
        {
            UserId = new UserId(10033),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 5, 21, 10, 0, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("harper.replica"),
            Email = new EmailAddress("harper@example.com"),
            DisplayName = new DisplayName("Harper Replica"),
        },
        new UserVersion
        {
            UserId = new UserId(10034),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 5, 28, 16, 15, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("hayden.ipsum"),
            Email = new EmailAddress("hayden@example.org"),
            DisplayName = new DisplayName("Hayden Ipsum"),
        },
        new UserVersion
        {
            UserId = new UserId(10035),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 6, 3, 9, 30, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.TaylorDummy,
            Username = new Username("indy.generic"),
            Email = new EmailAddress("indy@example.net"),
            DisplayName = new DisplayName("Indy Generic"),
        },
        new UserVersion
        {
            UserId = new UserId(10036),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 6, 11, 12, 40, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("jesse.default"),
            Email = new EmailAddress("jesse@example.com"),
            DisplayName = new DisplayName("Jesse Default"),
        },
        new UserVersion
        {
            UserId = new UserId(10037),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 6, 24, 15, 55, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("jules.preview"),
            Email = new EmailAddress("jules@example.org"),
            DisplayName = new DisplayName("Jules Preview"),
        },
        new UserVersion
        {
            UserId = new UserId(10038),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 7, 2, 10, 10, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.TaylorDummy,
            Username = new Username("kai.seed"),
            Email = new EmailAddress("kai@example.net"),
            DisplayName = new DisplayName("Kai Seed"),
        },
        new UserVersion
        {
            UserId = new UserId(10039),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 7, 9, 13, 20, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("kendall.nominal"),
            Email = new EmailAddress("kendall@example.com"),
            DisplayName = new DisplayName("Kendall Nominal"),
        },
        new UserVersion
        {
            UserId = new UserId(10040),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 7, 16, 9, 45, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("lane.placebo"),
            Email = new EmailAddress("lane@example.org"),
            DisplayName = new DisplayName("Lane Placebo"),
        },
        new UserVersion
        {
            UserId = new UserId(10041),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 7, 28, 14, 30, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.TaylorDummy,
            Username = new Username("micah.facsimile"),
            Email = new EmailAddress("micah@example.net"),
            DisplayName = new DisplayName("Micah Facsimile"),
        },
        new UserVersion
        {
            UserId = new UserId(10042),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 8, 5, 11, 5, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("nico.surrogate"),
            Email = new EmailAddress("nico@example.com"),
            DisplayName = new DisplayName("Nico Surrogate"),
        },
        new UserVersion
        {
            UserId = new UserId(10043),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 8, 13, 16, 40, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("noel.variant"),
            Email = new EmailAddress("noel@example.org"),
            DisplayName = new DisplayName("Noel Variant"),
        },
        new UserVersion
        {
            UserId = new UserId(10044),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 8, 21, 10, 25, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.TaylorDummy,
            Username = new Username("oakley.outline"),
            Email = new EmailAddress("oakley@example.net"),
            DisplayName = new DisplayName("Oakley Outline"),
        },
        new UserVersion
        {
            UserId = new UserId(10045),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 8, 31, 13, 50, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("parker.pretend"),
            Email = new EmailAddress("parker@example.com"),
            DisplayName = new DisplayName("Parker Pretend"),
        },
        new UserVersion
        {
            UserId = new UserId(10046),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 9, 9, 9, 20, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("quinn.query"),
            Email = new EmailAddress("quinn@example.org"),
            DisplayName = new DisplayName("Quinn Query"),
        },
        new UserVersion
        {
            UserId = new UserId(10047),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 9, 18, 15, 35, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.TaylorDummy,
            Username = new Username("reese.rehearsal"),
            Email = new EmailAddress("reese@example.net"),
            DisplayName = new DisplayName("Reese Rehearsal"),
        },
        new UserVersion
        {
            UserId = new UserId(10048),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 9, 29, 11, 15, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("rowan.stand-in"),
            Email = new EmailAddress("rowan@example.com"),
            DisplayName = new DisplayName("Rowan Stand-In"),
        },
        new UserVersion
        {
            UserId = new UserId(10049),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = new DateTime(2026, 10, 13, 14, 0, 0, DateTimeKind.Utc),
            ChangedBy = UserSeed.DemoUser,
            Username = new Username("sage.stopgap"),
            Email = new EmailAddress("sage@example.org"),
            DisplayName = new DisplayName("Sage Stopgap"),
        },
    ];
}
