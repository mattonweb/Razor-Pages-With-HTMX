namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Collections.Generic;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What changed between two versions of an organization, as its History tab shows it: the fields whose values differ,
/// in the order the Details tab lists them, and the words that sum them up.
/// </summary>
public static class OrganizationChanges
{
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
    /// Names the fields that changed, as a summary reads them: Name, Name and Website, or Name, Industry and Website.
    /// </summary>
    /// <param name="changes">The fields that changed.</param>
    /// <returns>Their names, joined.</returns>
    public static string Fields(IReadOnlyList<FieldChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        string[] names = [.. changes.Select(static change => change.Field)];
        return names.Length < 2 ? string.Concat(names) : $"{string.Join(", ", names[..^1])} and {names[^1]}";
    }

    /// <summary>
    /// Names what a version records happening, as its entry in the timeline leads with it.
    /// </summary>
    /// <param name="change">What happened.</param>
    /// <returns>The words, such as Rolled back.</returns>
    public static string Title(ChangeKind change)
    {
        return change switch
        {
            ChangeKind.Created => "Created",
            ChangeKind.Edited => "Edited",
            ChangeKind.Archived => "Archived",
            ChangeKind.Restored => "Restored",
            _ => "Rolled back",
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
