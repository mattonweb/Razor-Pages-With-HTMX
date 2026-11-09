namespace HTMX.Application.Web.Pages.Contacts;

using System;
using System.Collections.Generic;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What changed between two versions of a contact, as their History tab shows it: the fields whose values differ, in
/// the order the Details tab lists them, and the timeline's entries made from them.
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
        bool active = contact.Archived is null && contact.Organization.ArchivedOn is null;
        string name = contact.Name.Value;
        return [.. versions.Select((version, index) => Entry(versions, index, active, name))];
    }

    /// <summary>
    /// Gives the fields a version changed from the one before it, its status included.
    /// </summary>
    /// <param name="before">The version before.</param>
    /// <param name="after">The version that made the change.</param>
    /// <returns>The fields that changed.</returns>
    public static FieldChange[] Between(ContactVersionResponse before, ContactVersionResponse after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        List<FieldChange> changes = [];
        AddValues(changes, before, after);
        Add(changes, "Status", StatusOf(before), StatusOf(after));
        return [.. changes];
    }

    /// <summary>
    /// Gives the fields a roll-back would change: the values an earlier version held, against the values now, the
    /// organization among them. Status is not; a roll-back saves values, and only an active contact is rolled back.
    /// </summary>
    /// <param name="now">The current version.</param>
    /// <param name="earlier">The version to roll back to.</param>
    /// <returns>The fields that would change, none when the earlier version holds the same values.</returns>
    public static FieldChange[] RollBack(ContactVersionResponse now, ContactVersionResponse earlier)
    {
        ArgumentNullException.ThrowIfNull(now);
        ArgumentNullException.ThrowIfNull(earlier);
        List<FieldChange> changes = [];
        AddValues(changes, now, earlier);
        return [.. changes];
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
    /// Makes the timeline's entry for one version: what it changed from the version after it in the list, which is
    /// the one before it in time, and what rolling back to it would change now, or why it cannot be rolled back to.
    /// </summary>
    /// <param name="versions">Every version, newest first.</param>
    /// <param name="index">Which one.</param>
    /// <param name="active">Whether the contact and their organization are active, so the contact can be rolled
    /// back.</param>
    /// <param name="name">The contact's name now.</param>
    /// <returns>The entry.</returns>
    private static TimelineEntry Entry(
        IReadOnlyList<ContactVersionResponse> versions,
        int index,
        bool active,
        string name)
    {
        ContactVersionResponse version = versions[index];
        bool earlier = active && index > 0;
        string? unwelcome = earlier ? Unwelcome(versions[0], version, name) : null;
        FieldChange[] rollBack = earlier && unwelcome is null ? RollBack(versions[0], version) : [];
        bool same = earlier && unwelcome is null && rollBack.Length == 0;
        return new TimelineEntry
        {
            Version = version.Version,
            Change = version.Change,
            RolledBackTo = version.RolledBackTo,
            Changed = version.Changed,
            Changes = index + 1 < versions.Count ? Between(versions[index + 1], version) : [],
            RollBack = rollBack,
            Note = same ? $"Version {version.Version.Value} holds the same values as now." : unwelcome,
        };
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
    /// Adds the values a person edits that differ between two versions, in the Details tab's order.
    /// </summary>
    /// <param name="changes">The changes found so far.</param>
    /// <param name="before">The values before.</param>
    /// <param name="after">The values after.</param>
    private static void AddValues(
        List<FieldChange> changes,
        ContactVersionResponse before,
        ContactVersionResponse after)
    {
        Add(changes, "Name", before.Name.Value, after.Name.Value);
        Add(changes, "Organization", OrganizationOf(before), OrganizationOf(after));
        Add(changes, "Job title", before.JobTitle?.Value, after.JobTitle?.Value);
        Add(changes, "Email", before.Email?.Value, after.Email?.Value);
        Add(changes, "Phone", before.Phone?.Value, after.Phone?.Value);
        Add(changes, "Notes", before.Notes?.Value, after.Notes?.Value);
    }

    /// <summary>
    /// Adds one field, if its value changed.
    /// </summary>
    /// <param name="changes">The changes found so far.</param>
    /// <param name="field">The field's name.</param>
    /// <param name="before">Its value before, if any.</param>
    /// <param name="after">Its value after, if any.</param>
    private static void Add(List<FieldChange> changes, string field, string? before, string? after)
    {
        if (string.Equals(before, after, StringComparison.Ordinal))
        {
            return;
        }

        changes.Add(new FieldChange { Field = field, Before = before, After = after });
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
    /// Says whether a version left the contact active or archived.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>Active or Archived.</returns>
    private static string StatusOf(ContactVersionResponse version)
    {
        return version.ArchivedOn is null ? "Active" : "Archived";
    }
}
