namespace HTMX.Application.Web.Pages;

using System;
using System.Collections.Generic;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The words a timeline entry leads with, for any kind of record: what happened, and the summary beside it.
/// </summary>
public static class TimelineText
{
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
    /// Sums up an entry after its title: the fields an edit changed, or the version a roll-back went back to.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <returns>The summary, empty when the title says it all.</returns>
    public static string Summary(TimelineEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.Change switch
        {
            ChangeKind.Edited => Fields(entry.Changes),
            ChangeKind.RolledBack => $"to version {entry.RolledBackTo?.Value}",
            _ => string.Empty,
        };
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
}
