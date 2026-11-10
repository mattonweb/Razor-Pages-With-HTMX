namespace HTMX.Application.Web.Pages.Users;

using System;
using System.Collections.Generic;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What changed between two versions of a user, as their History tab shows it: the fields whose values differ, in the
/// order the page shows them, and the timeline's entries made from them.
/// </summary>
public static class UserChanges
{
    /// <summary>
    /// Turns a user's versions into their timeline's entries, newest first: what each version changed from the one
    /// before it and, while the user is active, what rolling back to each earlier version would change.
    /// </summary>
    /// <param name="account">The user as they are now.</param>
    /// <param name="versions">Their versions, newest first.</param>
    /// <returns>The entries, in the same order.</returns>
    public static TimelineEntry[] Entries(UserSingleResponse account, IReadOnlyList<UserVersionResponse> versions)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(versions);
        bool active = account.Archived is null;
        return [.. versions.Select((version, index) => Entry(versions, index, active))];
    }

    /// <summary>
    /// Gives the fields a version changed from the one before it, its status included.
    /// </summary>
    /// <param name="before">The version before.</param>
    /// <param name="after">The version that made the change.</param>
    /// <returns>The fields that changed.</returns>
    public static FieldChange[] Between(UserVersionResponse before, UserVersionResponse after)
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
    /// is not among them; a roll-back saves values, and only an active user is rolled back.
    /// </summary>
    /// <param name="now">The current version.</param>
    /// <param name="earlier">The version to roll back to.</param>
    /// <returns>The fields that would change, none when the earlier version holds the same values.</returns>
    public static FieldChange[] RollBack(UserVersionResponse now, UserVersionResponse earlier)
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
    /// <param name="active">Whether the user is active, and so can be rolled back.</param>
    /// <returns>The entry.</returns>
    private static TimelineEntry Entry(IReadOnlyList<UserVersionResponse> versions, int index, bool active)
    {
        UserVersionResponse version = versions[index];
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
    /// Adds the values that differ between two versions: name, username and email address.
    /// </summary>
    /// <param name="changes">The changes found so far.</param>
    /// <param name="before">The values before.</param>
    /// <param name="after">The values after.</param>
    private static void AddValues(List<FieldChange> changes, UserVersionResponse before, UserVersionResponse after)
    {
        Add(changes, "Name", before.DisplayName.Value, after.DisplayName.Value);
        Add(changes, "Username", before.Username.Value, after.Username.Value);
        Add(changes, "Email", before.Email.Value, after.Email.Value);
    }

    /// <summary>
    /// Adds one field, if its value changed.
    /// </summary>
    /// <param name="changes">The changes found so far.</param>
    /// <param name="field">The field's name.</param>
    /// <param name="before">Its value before.</param>
    /// <param name="after">Its value after.</param>
    private static void Add(List<FieldChange> changes, string field, string before, string after)
    {
        if (string.Equals(before, after, StringComparison.Ordinal))
        {
            return;
        }

        changes.Add(new FieldChange { Field = field, Before = before, After = after });
    }

    /// <summary>
    /// Says whether a version left the user active or archived.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>Active or Archived.</returns>
    private static string StatusOf(UserVersionResponse version)
    {
        return version.ArchivedOn is null ? "Active" : "Archived";
    }
}
