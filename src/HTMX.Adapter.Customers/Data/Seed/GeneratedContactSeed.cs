namespace HTMX.Adapter.Customers.Data.Seed;

using System;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// The 1,492 contacts that give the organizations their people: 1,500 in all with the 8 in <see cref="ContactSeed"/>.
/// Every run builds the same ones.
/// </summary>
/// <remarks>
/// <para>
/// Placeholder Logistics gets 16, so its own list of contacts pages. Each generated organization gets none to three,
/// by its position, so the hierarchy shows organizations with no contacts, one, and several. The other organizations
/// written out get none here.
/// </para>
/// <para>
/// Each name pairs a word from <see cref="FirstNames"/> with one from <see cref="LastNames"/>. No first name is used by
/// a user or by the 8, so every name is unique and obviously invented. A contact joins their organization while it is
/// active: between its creation and its archiving, or an hour before the application started. Every 9th has no email,
/// every 3rd no phone, every 5th no job title, and every 25th is archived, 59 in all.
/// </para>
/// </remarks>
internal static class GeneratedContactSeed
{
    private const long FirstIdentifier = 10009;
    private const int PlaceholderLogisticsContacts = 16;
    private const int MostContactsPerOrganization = 3;
    private const int NoEmailEvery = 9;
    private const int NoPhoneEvery = 3;
    private const int NoJobTitleEvery = 5;
    private const int ArchiveEvery = 25;

    private static readonly string[] FirstNames =
    [
        "Arden", "Ash", "Bowie", "Briar", "Brook", "Cody", "Dale", "Ellery", "Emerson", "Florian",
        "Fran", "Glen", "Hale", "Iris", "Jody", "Jude", "Kerry", "Laurie", "Linden", "Lou",
        "Mackenzie", "Marley", "Merritt", "Nat", "Oli", "Perry", "Rae", "Remy", "Ripley", "Robbie",
        "Sasha", "Shiloh", "Sidney", "Sky", "Sterling", "Sutton", "Tam", "Tobin", "Vic", "Wynn",
    ];

    private static readonly string[] LastNames =
    [
        "Abstract", "Backfill", "Benchmark", "Casefile", "Checkpoint", "Constant", "Dataset", "Edgecase",
        "Exemplar", "Fallback", "Footnote", "Gizmo", "Handout", "Instance", "Keyframe", "Macro",
        "Marker", "Notation", "Overlay", "Paragon", "Pastiche", "Pattern", "Payload", "Placard",
        "Pseudonym", "Readme", "Redraft", "Rubric", "Sampler", "Snippet", "Standby", "Stencil",
        "Swatch", "Tagline", "Thumbnail", "Token", "Tracer", "Understudy", "Vignette", "Widget",
    ];

    private static readonly string[] JobTitles =
    [
        "Chief Demonstration Officer", "Director of Dummy Data", "Draft Editor", "Fixture Specialist",
        "Head of Examples", "Lead Mock Engineer", "Placeholder Manager", "Prototype Analyst",
        "Sample Coordinator", "Senior Stand-In", "Template Designer", "Test Case Planner",
    ];

    // Taylor Dummy was created 187 days before the application started; Taylor creates only younger contacts.
    private static readonly TimeSpan TaylorCreatesYoungerThan = TimeSpan.FromDays(180);

    // No contact joins in the last hour, which belongs to the changes written out by hand.
    private static readonly TimeSpan QuietHour = TimeSpan.FromHours(1);

    /// <summary>
    /// Creates every version of the 1,492 contacts: one each, and a second for each archived one.
    /// </summary>
    /// <param name="startedOn">When the application started; every date is written as a time before it.</param>
    /// <param name="organizations">Every seed organization, as its current row, so each contact joins while it is
    /// active.</param>
    /// <returns>New instances on every call.</returns>
    internal static ContactVersion[] CreateVersions(DateTime startedOn, Organization[] organizations)
    {
        ArgumentNullException.ThrowIfNull(organizations);

        // One entry per contact: an organization appears once for each contact it gets.
        Organization[] placements =
        [
            .. organizations
                .OrderBy(static organization => organization.Id.Value)
                .SelectMany(static organization => Enumerable.Repeat(organization, CountContacts(organization))),
        ];
        return
        [
            .. Enumerable.Range(0, placements.Length)
                .SelectMany(index => CreateHistory(index, placements[index], startedOn)),
        ];
    }

    /// <summary>
    /// Says how many generated contacts an organization gets.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>16 for Placeholder Logistics, none to three for a generated organization, and none otherwise.</returns>
    private static int CountContacts(Organization organization)
    {
        if (organization.Id == OrganizationSeed.PlaceholderLogistics)
        {
            return PlaceholderLogisticsContacts;
        }

        long position = organization.Id.Value - GeneratedOrganizationSeed.FirstIdentifier;
        return position < 0 ? 0 : (int)(position % (MostContactsPerOrganization + 1));
    }

    /// <summary>
    /// Creates one contact's versions: created, and for every 25th contact, archived halfway between joining and the
    /// application's start.
    /// </summary>
    /// <param name="index">The contact's position, from 0.</param>
    /// <param name="organization">The organization the contact joins.</param>
    /// <param name="startedOn">When the application started.</param>
    /// <returns>The contact's versions, oldest first.</returns>
    private static ContactVersion[] CreateHistory(int index, Organization organization, DateTime startedOn)
    {
        ContactVersion created = CreateFirstVersion(index, organization, startedOn);
        if (index % ArchiveEvery != ArchiveEvery - 1)
        {
            return [created];
        }

        DateTime archivedOn = created.ChangedOn + ((startedOn - created.ChangedOn) / 2);
        ContactVersion archived = created with
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
    /// Creates the version that created one contact: their name, details, organization, date and creator, all worked
    /// out from their position.
    /// </summary>
    /// <param name="index">The contact's position, from 0.</param>
    /// <param name="organization">The organization the contact joins.</param>
    /// <param name="startedOn">When the application started.</param>
    /// <returns>The contact's first version.</returns>
    private static ContactVersion CreateFirstVersion(int index, Organization organization, DateTime startedOn)
    {
        // Every index gets a different pair of names, as for the generated organizations.
        string first = FirstNames[index % FirstNames.Length];
        string last = LastNames[((index / FirstNames.Length) + (index % FirstNames.Length)) % LastNames.Length];
        DateTime joinedOn = ChooseJoiningDate(index, organization, startedOn);
        return new ContactVersion
        {
            ContactId = new ContactId(FirstIdentifier + index),
            Version = new VersionNumber(1),
            Change = ChangeKind.Created,
            ChangedOn = joinedOn,
            ChangedBy = ChooseCreator(index, startedOn - joinedOn),
            Name = new DisplayName($"{first} {last}"),
            Email = index % NoEmailEvery == NoEmailEvery - 1 ? null : CreateEmail(first, last, organization),
            Phone = index % NoPhoneEvery == NoPhoneEvery - 1 ? null : new PhoneNumber($"+1 555-01{index % 100:00}"),
            JobTitle = index % NoJobTitleEvery == NoJobTitleEvery - 1 ? null : ChooseJobTitle(index),
            OrganizationId = organization.Id,
        };
    }

    /// <summary>
    /// Chooses when a contact joined: somewhere between their organization's creation and the end of its active life,
    /// spread so that no two contacts of one organization join at the same moment.
    /// </summary>
    /// <param name="index">The contact's position, from 0.</param>
    /// <param name="organization">The organization the contact joins.</param>
    /// <param name="startedOn">When the application started.</param>
    /// <returns>The moment the contact was created.</returns>
    private static DateTime ChooseJoiningDate(int index, Organization organization, DateTime startedOn)
    {
        DateTime activeUntil = organization.ArchivedOn ?? startedOn - QuietHour;
        double fraction = (((index * 37) % 97) + 1) / 98.0;
        return organization.CreatedOn + ((activeUntil - organization.CreatedOn) * fraction);
    }

    /// <summary>
    /// Chooses who created a contact: Taylor Dummy for every third one young enough, Demo User otherwise.
    /// </summary>
    /// <param name="index">The contact's position, from 0.</param>
    /// <param name="age">How long before the application started the contact was created.</param>
    /// <returns>The creator.</returns>
    private static UserId ChooseCreator(int index, TimeSpan age)
    {
        return index % 3 == 0 && age < TaylorCreatesYoungerThan ? UserSeed.TaylorDummy : UserSeed.DemoUser;
    }

    /// <summary>
    /// Chooses a contact's job title from the list, by their position.
    /// </summary>
    /// <param name="index">The contact's position, from 0.</param>
    /// <returns>The job title.</returns>
    private static JobTitle ChooseJobTitle(int index)
    {
        return new JobTitle(JobTitles[index % JobTitles.Length]);
    }

    /// <summary>
    /// Makes a contact's email address at their organization's website, such as ash.abstract@example-freight.example,
    /// or at the organization's name when it has no website.
    /// </summary>
    /// <param name="first">The contact's first name.</param>
    /// <param name="last">The contact's last name.</param>
    /// <param name="organization">The organization the contact belongs to.</param>
    /// <returns>The email address.</returns>
    private static EmailAddress CreateEmail(string first, string last, Organization organization)
    {
        string host = organization.Website is WebAddress website
            ? new Uri(website.Value).Host
            : $"{OrganizationHostName(organization.Name)}.example";
        string address = $"{first}.{last}@{host}";
        return new EmailAddress(address.ToLowerInvariant());
    }

    /// <summary>
    /// Turns an organization's name into a host name, such as Fixture-Foods for Fixture Foods.
    /// </summary>
    /// <param name="name">The organization's name.</param>
    /// <returns>The name with its spaces as hyphens.</returns>
    private static string OrganizationHostName(OrganizationName name)
    {
        return name.Value.Replace(' ', '-');
    }
}
