namespace HTMX.Adapter.Customers.Data.Seed;

using System;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// The 985 organizations that make the list long enough to page through properly: 1,000 in all with the 15 in
/// <see cref="OrganizationSeed"/>. Every run builds the same ones.
/// </summary>
/// <remarks>
/// Each name pairs a word from <see cref="FirstWords"/> with one from <see cref="LastWords"/>, such as Example Freight,
/// so every name is invented and obviously so, and none starts with a word the 15 use. The last word decides the
/// industry. Every 7th organization has no website, and every 20th is archived, 49 in all, so the Archived filter
/// pages too. Each was created between an hour and 230 days before the application started, no two at the same
/// minute, so the list sorted by Updated never ties.
/// </remarks>
internal static class GeneratedOrganizationSeed
{
    /// <summary>
    /// The identifier of the first generated organization; the 15 written out come before it.
    /// </summary>
    internal const long FirstIdentifier = 10016;

    private const int Count = 985;
    private const int LastWordsPerIndustry = 5;
    private const int NoWebsiteEvery = 7;
    private const int ArchiveEvery = 20;
    private const int ArchiveOffset = 7;

    // Ages step through 230 days 337 minutes at a time. 337 shares no factor with the minutes in 230 days, so no two
    // organizations land on the same minute.
    private const int MinimumAgeInMinutes = 60;
    private const int AgeStepInMinutes = 337;
    private const int AgeRangeInMinutes = 230 * 24 * 60;

    private static readonly string[] FirstWords =
    [
        "Blueprint", "Boilerplate", "Decoy", "Default", "Ersatz", "Example", "Fabricated", "Facsimile",
        "Faux", "Filler", "Generic", "Hypothetical", "Imaginary", "Imitation", "Interim", "Invented",
        "Lookalike", "Makeshift", "Mock", "Nominal", "Notional", "Outline", "Pilot", "Placebo",
        "Pretend", "Preview", "Provisional", "Proxy", "Rehearsal", "Replica", "Scaffold", "Simulated",
        "Sketch", "Stand-In", "Stopgap", "Surrogate", "Tentative", "Trial", "Variant", "Wireframe",
    ];

    // Five last words per industry, in the order of LastWordIndustries.
    private static readonly string[] LastWords =
    [
        "Couriers", "Freight", "Haulage", "Logistics", "Shipping",
        "Bakery", "Creamery", "Foods", "Kitchens", "Provisions",
        "Fabrication", "Foundry", "Mills", "Tooling", "Works",
        "Broadcasting", "Pictures", "Press", "Publishing", "Studios",
        "Emporium", "Goods", "Market", "Outfitters", "Traders",
        "Data", "Labs", "Networks", "Software", "Systems",
    ];

    private static readonly Industry[] LastWordIndustries =
    [
        Industry.Shipping,
        Industry.FoodAndDrink,
        Industry.Manufacturing,
        Industry.Media,
        Industry.Retail,
        Industry.Technology,
    ];

    // Taylor Dummy was created 187 days before the application started; Taylor creates only younger organizations.
    private static readonly TimeSpan TaylorCreatesYoungerThan = TimeSpan.FromDays(180);

    /// <summary>
    /// Creates every version of the 985 organizations: one each, and a second for each archived one.
    /// </summary>
    /// <param name="startedOn">When the application started; every date is written as a time before it.</param>
    /// <returns>New instances on every call.</returns>
    internal static OrganizationVersion[] CreateVersions(DateTime startedOn)
    {
        return [.. Enumerable.Range(0, Count).SelectMany(index => CreateHistory(index, startedOn))];
    }

    /// <summary>
    /// Creates one organization's versions: created, and for every 20th organization, archived halfway between its
    /// creation and the application's start.
    /// </summary>
    /// <param name="index">The organization's position, from 0.</param>
    /// <param name="startedOn">When the application started.</param>
    /// <returns>The organization's versions, oldest first.</returns>
    private static OrganizationVersion[] CreateHistory(int index, DateTime startedOn)
    {
        OrganizationVersion created = CreateFirstVersion(index, startedOn);
        if (index % ArchiveEvery != ArchiveOffset)
        {
            return [created];
        }

        DateTime archivedOn = created.ChangedOn + ((startedOn - created.ChangedOn) / 2);
        OrganizationVersion archived = created with
        {
            Version = new VersionNumber(2),
            Change = ChangeKind.Archived,
            ChangedOn = archivedOn,
            ChangedBy = UserSeed.DemoUser,
            ArchivedOn = archivedOn,
            ArchivedBy = UserSeed.DemoUser,
        };
        return [created, archived];
    }

    /// <summary>
    /// Creates the version that created one organization: its name, industry, website, age and creator, all worked out
    /// from its position.
    /// </summary>
    /// <param name="index">The organization's position, from 0.</param>
    /// <param name="startedOn">When the application started.</param>
    /// <returns>The organization's first version.</returns>
    private static OrganizationVersion CreateFirstVersion(int index, DateTime startedOn)
    {
        // Every index gets a different pair of words: the first word cycles, and the last word moves on by one each
        // time the first words start again.
        int first = index % FirstWords.Length;
        int last = ((index / FirstWords.Length) + first) % LastWords.Length;
        string name = $"{FirstWords[first]} {LastWords[last]}";
        TimeSpan age = TimeSpan.FromMinutes(MinimumAgeInMinutes + ((index * AgeStepInMinutes) % AgeRangeInMinutes));
        return new OrganizationVersion
        {
            OrganizationId = new OrganizationId(FirstIdentifier + index),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = startedOn - age,
            ChangedBy = ChooseCreator(index, age),
            Name = new OrganizationName(name),
            Industry = LastWordIndustries[last / LastWordsPerIndustry],
            Website = index % NoWebsiteEvery == 0 ? null : CreateWebsite(name),
        };
    }

    /// <summary>
    /// Chooses who created an organization: Taylor Dummy for every third one young enough, Demo User otherwise.
    /// </summary>
    /// <param name="index">The organization's position, from 0.</param>
    /// <param name="age">How long before the application started the organization was created.</param>
    /// <returns>The creator.</returns>
    private static UserId ChooseCreator(int index, TimeSpan age)
    {
        return index % 3 == 0 && age < TaylorCreatesYoungerThan ? UserSeed.TaylorDummy : UserSeed.DemoUser;
    }

    /// <summary>
    /// Makes an organization's website from its name, such as https://example-freight.example.
    /// </summary>
    /// <param name="name">The organization's name.</param>
    /// <returns>The website.</returns>
    private static WebAddress CreateWebsite(string name)
    {
        string host = name.Replace(' ', '-').ToLowerInvariant();
        return new WebAddress($"https://{host}.example");
    }
}
