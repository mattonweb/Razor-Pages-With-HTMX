namespace HTMX.Application.Web.Pages.Users;

using System;
using System.Collections.Generic;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A user's side of their History tab: the values a version holds, in the order the page shows them, and when the
/// user can be rolled back. <see cref="RecordChanges"/> compares the versions, as it does every record's.
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
        return RecordChanges.Entries([.. versions.Select(Describe)], active);
    }

    /// <summary>
    /// Describes one version for the History tab to compare: the values a person edits, which are name, username and
    /// email address, and whether it left the user archived.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>The version as every record's History tab compares it.</returns>
    public static HistoryVersion Describe(UserVersionResponse version)
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
                new FieldValue { Field = "Name", Value = version.DisplayName.Value },
                new FieldValue { Field = "Username", Value = version.Username.Value },
                new FieldValue { Field = "Email", Value = version.Email.Value },
            ],
        };
    }
}
