namespace HTMX.Application.Web.Pages.Contacts;

using System;
using System.Collections.Generic;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A contact's side of their History tab: the values a version holds, in the order the Details tab lists them, the
/// organization among them, when the contact can be rolled back, and which versions they can't go back to whatever
/// their values. <see cref="RecordChanges"/> compares the versions, as it does every record's.
/// </summary>
public static class ContactChanges
{
    /// <summary>
    /// Turns a contact's versions into their timeline's entries, newest first: what each version changed from the one
    /// before it and, while the contact and their organization are active, what rolling back to each earlier version
    /// would change. A version that names another organization, one that is archived or gone, says so instead.
    /// </summary>
    /// <param name="contact">The contact as they are now.</param>
    /// <param name="versions">Their versions, newest first.</param>
    /// <returns>The entries, in the same order.</returns>
    public static TimelineEntry[] Entries(ContactSingleResponse contact, IReadOnlyList<ContactVersionResponse> versions)
    {
        ArgumentNullException.ThrowIfNull(contact);
        ArgumentNullException.ThrowIfNull(versions);
        if (versions.Count == 0)
        {
            return [];
        }

        bool active = contact.Archived is null && contact.Organization.ArchivedOn is null;
        ContactVersionResponse now = versions[0];
        string name = contact.Name.Value;
        return RecordChanges.Entries([.. versions.Select(version => Describe(version, now, name))], active);
    }

    /// <summary>
    /// Describes one version for the History tab to compare: the values a person edits, in the Details tab's order,
    /// and whether it left the contact archived. The organization is named as it is now, or by its identifier once it
    /// is deleted.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>The version as every record's History tab compares it.</returns>
    public static HistoryVersion Describe(ContactVersionResponse version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return new HistoryVersion
        {
            Version = version.Version,
            Change = version.Change,
            RolledBackTo = version.RolledBackTo,
            Changed = version.Changed,
            ArchivedOn = version.ArchivedOn,
            Values =
            [
                new FieldValue { Field = "Name", Value = version.Name.Value },
                new FieldValue { Field = "Organization", Value = OrganizationOf(version) },
                new FieldValue { Field = "Job title", Value = version.JobTitle?.Value },
                new FieldValue { Field = "Email", Value = version.Email?.Value },
                new FieldValue { Field = "Phone numbers", Value = PhoneNumbersOf(version) },
                new FieldValue { Field = "Notes", Value = version.Notes?.Value },
            ],
        };
    }

    /// <summary>
    /// Says why a contact cannot go back to a version whose organization takes no one in: it is archived, or it no
    /// longer exists. The timeline says it in place of Roll back, and the toast says it when a roll-back is refused.
    /// </summary>
    /// <param name="earlier">The version that cannot be rolled back to.</param>
    /// <param name="name">The contact's name now.</param>
    /// <returns>Why.</returns>
    public static string Unavailable(ContactVersionResponse earlier, string name)
    {
        ArgumentNullException.ThrowIfNull(earlier);
        int number = earlier.Version.Value;
        return earlier.Organization is OrganizationReference organization
            ? $"{organization.Name.Value} is archived, so {name} can't go back to version {number}."
            : $"The organization of version {number} no longer exists, so {name} can't go back to it.";
    }

    /// <summary>
    /// Describes one version, and whether the contact can go back to it from the version they are at now.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="now">The version the contact is at now.</param>
    /// <param name="name">The contact's name now.</param>
    /// <returns>The version as every record's History tab compares it.</returns>
    private static HistoryVersion Describe(ContactVersionResponse version, ContactVersionResponse now, string name)
    {
        return Describe(version) with { RollBackUnavailable = Unwelcome(now, version, name) };
    }

    /// <summary>
    /// Says why a version cannot be rolled back to when it names another organization that takes no one in.
    /// </summary>
    /// <param name="now">The current version.</param>
    /// <param name="earlier">The version to roll back to.</param>
    /// <param name="name">The contact's name now.</param>
    /// <returns>Why, or <see langword="null"/> when the organization is no reason not to.</returns>
    private static string? Unwelcome(ContactVersionResponse now, ContactVersionResponse earlier, string name)
    {
        bool staying = earlier.OrganizationId == now.OrganizationId;
        bool open = earlier.Organization is OrganizationReference organization && organization.ArchivedOn is null;
        return staying || open ? null : Unavailable(earlier, name);
    }

    /// <summary>
    /// Names the organization a version names: by its name now, or, once it is deleted, by its identifier.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>The words.</returns>
    private static string OrganizationOf(ContactVersionResponse version)
    {
        return version.Organization?.Name.Value ?? $"Deleted organization {version.OrganizationId.Value}";
    }

    /// <summary>
    /// Writes the telephone numbers a version holds as one value, in their order, so that adding, removing or
    /// reordering a number shows as a change to it.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>The numbers, separated by commas, or <see langword="null"/> for none.</returns>
    private static string? PhoneNumbersOf(ContactVersionResponse version)
    {
        PhoneNumber[] numbers = version.PhoneNumbers.Values;
        return numbers.Length == 0 ? null : string.Join(", ", Array.ConvertAll(numbers, static number => number.Value));
    }
}
