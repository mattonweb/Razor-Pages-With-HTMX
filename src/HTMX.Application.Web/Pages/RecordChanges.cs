namespace HTMX.Application.Web.Pages;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// What changed between two versions of a record, the same for every record: the fields whose values differ, in the
/// order the Details tab lists them, and the History tab's timeline entries made from them. Each record's changes
/// class, such as <see cref="Organizations.OrganizationChanges"/>, says which fields it has and whether the record
/// can be rolled back.
/// </summary>
public static class RecordChanges
{
    /// <summary>
    /// Turns a record's versions into its timeline's entries, newest first: what each version changed from the one
    /// before it and, while the record can be rolled back, what rolling back to each earlier version would change, or
    /// why it can't go back to that one.
    /// </summary>
    /// <param name="versions">The record's versions, newest first.</param>
    /// <param name="active">Whether the record can be rolled back now, which needs it active.</param>
    /// <returns>The entries, in the same order.</returns>
    public static TimelineEntry[] Entries(IReadOnlyList<HistoryVersion> versions, bool active)
    {
        ArgumentNullException.ThrowIfNull(versions);
        return [.. versions.Select((version, index) => Entry(versions, index, active))];
    }

    /// <summary>
    /// Gives the fields a version changed from the one before it, its status included.
    /// </summary>
    /// <param name="before">The version before.</param>
    /// <param name="after">The version that made the change.</param>
    /// <returns>The fields that changed.</returns>
    public static FieldChange[] Between(HistoryVersion before, HistoryVersion after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        return Differences([.. before.Values, StatusOf(before)], [.. after.Values, StatusOf(after)]);
    }

    /// <summary>
    /// Gives the fields a roll-back would change: the values an earlier version held, against the values now. Status
    /// is not among them; a roll-back saves values, and only an active record is rolled back.
    /// </summary>
    /// <param name="now">The current version.</param>
    /// <param name="earlier">The version to roll back to.</param>
    /// <returns>The fields that would change, none when the earlier version holds the same values.</returns>
    public static FieldChange[] RollBack(HistoryVersion now, HistoryVersion earlier)
    {
        ArgumentNullException.ThrowIfNull(now);
        ArgumentNullException.ThrowIfNull(earlier);
        return Differences(now.Values, earlier.Values);
    }

    /// <summary>
    /// Makes the timeline's entry for one version: what it changed from the version after it in the list, which is
    /// the one before it in time, and what rolling back to it would change now, or why it can't be rolled back to.
    /// </summary>
    /// <param name="versions">Every version, newest first.</param>
    /// <param name="index">Which one.</param>
    /// <param name="active">Whether the record can be rolled back now.</param>
    /// <returns>The entry.</returns>
    private static TimelineEntry Entry(IReadOnlyList<HistoryVersion> versions, int index, bool active)
    {
        HistoryVersion version = versions[index];
        bool earlier = active && index > 0;
        string? unavailable = earlier ? version.RollBackUnavailable : null;
        FieldChange[] rollBack = earlier && unavailable is null ? RollBack(versions[0], version) : [];
        bool same = earlier && unavailable is null && rollBack.Length == 0;
        return new TimelineEntry
        {
            Version = version.Version,
            Change = version.Change,
            RolledBackTo = version.RolledBackTo,
            Changed = version.Changed,
            Changes = index + 1 < versions.Count ? Between(versions[index + 1], version) : [],
            RollBack = rollBack,
            Note = same ? $"Version {version.Version.Value} holds the same values as now." : unavailable,
        };
    }

    /// <summary>
    /// Gives the fields whose values differ between two lists of the same fields, in their order.
    /// </summary>
    /// <param name="before">The values before.</param>
    /// <param name="after">The values after, field for field.</param>
    /// <returns>The fields that differ.</returns>
    private static FieldChange[] Differences(FieldValue[] before, FieldValue[] after)
    {
        return
        [
            .. before
                .Zip(
                    after,
                    static (was, now) => new FieldChange { Field = now.Field, Before = was.Value, After = now.Value })
                .Where(static change => !string.Equals(change.Before, change.After, StringComparison.Ordinal)),
        ];
    }

    /// <summary>
    /// Says whether a version left the record active or archived, as one more field to compare.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>The Status field, Active or Archived.</returns>
    private static FieldValue StatusOf(HistoryVersion version)
    {
        return new FieldValue { Field = "Status", Value = version.ArchivedOn is null ? "Active" : "Archived" };
    }
}
