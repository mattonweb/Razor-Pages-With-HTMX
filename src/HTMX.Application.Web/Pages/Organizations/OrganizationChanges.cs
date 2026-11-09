namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Collections.Generic;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What changed between two versions of an organization, as its History tab shows it: the fields whose values differ,
/// in the order the Details tab lists them, and the timeline's entries made from them.
/// </summary>
public static class OrganizationChanges
{
    /// <summary>
    /// Turns an organization's versions into its timeline's entries, newest first: what each version changed from the
    /// one before it and, while the organization is active, what rolling back to each earlier version would change.
    /// </summary>
    /// <param name="organization">The organization as it is now.</param>
    /// <param name="versions">Its versions, newest first.</param>
    /// <returns>The entries, in the same order.</returns>
    public static TimelineEntry[] Entries(
        OrganizationSingleResponse organization,
        IReadOnlyList<OrganizationVersionResponse> versions)
    {
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(versions);
        bool active = organization.Archived is null;
        return [.. versions.Select((version, index) => Entry(versions, index, active))];
    }

    /// <summary>
    /// Gives the fields a version changed from the one before it, its status included.
    /// </summary>
    /// <param name="before">The version before.</param>
    /// <param name="after">The version that made the change.</param>
    /// <returns>The fields that changed.</returns>
    public static FieldChange[] Between(OrganizationVersionResponse before, OrganizationVersionResponse after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        List<FieldChange> changes = [];
        AddValues(changes, before, after);
        Add(changes, "Status", StatusOf(before), StatusOf(after));
        return [.. changes];
    }

    /// <summary>
    /// Gives the fields a roll-back would change: the values an earlier version held, against the values now. Status
    /// is not among them; a roll-back saves values, and only an active organization is rolled back.
    /// </summary>
    /// <param name="now">The current version.</param>
    /// <param name="earlier">The version to roll back to.</param>
    /// <returns>The fields that would change, none when the earlier version holds the same values.</returns>
    public static FieldChange[] RollBack(OrganizationVersionResponse now, OrganizationVersionResponse earlier)
    {
        ArgumentNullException.ThrowIfNull(now);
        ArgumentNullException.ThrowIfNull(earlier);
        List<FieldChange> changes = [];
        AddValues(changes, now, earlier);
        return [.. changes];
    }

    /// <summary>
    /// Makes the timeline's entry for one version: what it changed from the version after it in the list, which is
    /// the one before it in time, and what rolling back to it would change now.
    /// </summary>
    /// <param name="versions">Every version, newest first.</param>
    /// <param name="index">Which one.</param>
    /// <param name="active">Whether the organization is active, and so can be rolled back.</param>
    /// <returns>The entry.</returns>
    private static TimelineEntry Entry(IReadOnlyList<OrganizationVersionResponse> versions, int index, bool active)
    {
        OrganizationVersionResponse version = versions[index];
        bool earlier = active && index > 0;
        FieldChange[] rollBack = earlier ? RollBack(versions[0], version) : [];
        bool same = earlier && rollBack.Length == 0;
        return new TimelineEntry
        {
            Version = version.Version,
            Change = version.Change,
            RolledBackTo = version.RolledBackTo,
            Changed = version.Changed,
            Changes = index + 1 < versions.Count ? Between(versions[index + 1], version) : [],
            RollBack = rollBack,
            Note = same ? $"Version {version.Version.Value} holds the same values as now." : null,
        };
    }

    /// <summary>
    /// Adds the values a person edits that differ between two versions: name, industry and website.
    /// </summary>
    /// <param name="changes">The changes found so far.</param>
    /// <param name="before">The values before.</param>
    /// <param name="after">The values after.</param>
    private static void AddValues(
        List<FieldChange> changes,
        OrganizationVersionResponse before,
        OrganizationVersionResponse after)
    {
        Add(changes, "Name", before.Name.Value, after.Name.Value);
        Add(changes, "Industry", IndustryNames.Of(before.Industry), IndustryNames.Of(after.Industry));
        Add(changes, "Website", before.Website?.Value, after.Website?.Value);
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
    /// Says whether a version left the organization active or archived.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>Active or Archived.</returns>
    private static string StatusOf(OrganizationVersionResponse version)
    {
        return version.ArchivedOn is null ? "Active" : "Archived";
    }
}
