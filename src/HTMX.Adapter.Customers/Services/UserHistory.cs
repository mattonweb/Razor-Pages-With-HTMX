namespace HTMX.Adapter.Customers.Services;

using System;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// How each change to a user becomes their next version, and how their row then takes that version's values, as the
/// seed builds every row from its latest version. <see cref="UserService"/> decides whether a change may be made;
/// these only make the rows it saves, and need no query.
/// </summary>
internal static class UserHistory
{
    /// <summary>
    /// Makes a new user's row: the values sent, at version 1, created and last changed by whoever sent them.
    /// </summary>
    /// <param name="request">The new user.</param>
    /// <param name="now">When they are created, in UTC.</param>
    /// <returns>The row, without an identifier until the database gives it one.</returns>
    internal static User Start(UserCreateRequest request, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new User
        {
            Username = request.Username,
            Email = request.Email,
            DisplayName = request.DisplayName,
            CreatedOn = now,
            CreatedBy = request.CreatedBy,
            LastModifiedOn = now,
            LastModifiedBy = request.CreatedBy,
            Version = new VersionNumber(1),
        };
    }

    /// <summary>
    /// Makes the first version of a new user from their row as saved: a Created version holding the same values.
    /// </summary>
    /// <param name="user">The row, with the identifier the database gave it.</param>
    /// <returns>The version.</returns>
    internal static UserVersion RecordCreation(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserVersion
        {
            UserId = user.Id,
            Version = user.Version,
            Change = ChangeKind.Created,
            ChangedOn = user.CreatedOn,
            ChangedBy = user.CreatedBy,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
        };
    }

    /// <summary>
    /// Makes the version that records a change: the next number, the values sent, and who saved them and when.
    /// </summary>
    /// <param name="current">The user as they are now, who is active.</param>
    /// <param name="request">The change.</param>
    /// <param name="rolledBackTo">The earlier version the values came from, or <see langword="null"/> for an
    /// edit.</param>
    /// <param name="now">When the change is saved, in UTC.</param>
    /// <returns>The version.</returns>
    internal static UserVersion Record(
        User current,
        UserUpdateRequest request,
        VersionNumber? rolledBackTo,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(request);
        return new UserVersion
        {
            UserId = current.Id,
            Version = new VersionNumber(current.Version.Value + 1),
            Change = rolledBackTo is null ? ChangeKind.Edited : ChangeKind.RolledBack,
            RolledBackTo = rolledBackTo,
            ChangedOn = now,
            ChangedBy = request.ChangedBy,
            Username = request.Username,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };
    }

    /// <summary>
    /// Makes the version that records a user being archived or restored: the next number, their values as they are,
    /// and when and by whom they were archived, or nothing there once they are restored.
    /// </summary>
    /// <param name="current">The user as they are now.</param>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="by">Who is archiving or restoring them.</param>
    /// <param name="now">When, in UTC.</param>
    /// <returns>The version.</returns>
    internal static UserVersion RecordStatusChange(User current, ChangeKind change, UserId by, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(current);
        bool archiving = change == ChangeKind.Archived;
        return new UserVersion
        {
            UserId = current.Id,
            Version = new VersionNumber(current.Version.Value + 1),
            Change = change,
            ChangedOn = now,
            ChangedBy = by,
            Username = current.Username,
            Email = current.Email,
            DisplayName = current.DisplayName,
            ArchivedOn = archiving ? now : null,
            ArchivedBy = archiving ? by : null,
        };
    }

    /// <summary>
    /// Gives a user's row the values of their newest version, keeping who created them and when.
    /// </summary>
    /// <param name="current">The row as it is now.</param>
    /// <param name="version">The newest version.</param>
    /// <returns>The row's new values.</returns>
    internal static User Apply(User current, UserVersion version)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(version);
        return current with
        {
            Username = version.Username,
            Email = version.Email,
            DisplayName = version.DisplayName,
            LastModifiedOn = version.ChangedOn,
            LastModifiedBy = version.ChangedBy,
            ArchivedOn = version.ArchivedOn,
            ArchivedBy = version.ArchivedBy,
            Version = version.Version,
        };
    }
}
