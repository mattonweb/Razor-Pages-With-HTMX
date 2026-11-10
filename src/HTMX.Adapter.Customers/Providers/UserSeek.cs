namespace HTMX.Adapter.Customers.Providers;

using System;
using System.Linq.Expressions;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;
using HTMX.Adapter.Customers.Extensions;

/// <summary>
/// The conditions that seek a page of the users list in key-based paging: those users who come after a cursor's, in
/// the order the page is read. <see cref="UserProvider"/> adds one to the query for any page that has a cursor.
/// </summary>
/// <remarks>
/// The same arrangement as <see cref="OrganizationSeek"/>. Each condition says "at least the key's value, and then past
/// the key", so that its first half is a range the column's index can seek to and the second only settles the ties at
/// its edge. Names, usernames, email addresses and identifiers are compared with the adapter's own extension methods,
/// such as <see cref="UsernameExtensions"/>, which Entity Framework turns into SQL on the bare columns.
/// </remarks>
internal static class UserSeek
{
    /// <summary>
    /// Keeps the users who come after the cursor's in the order the page is read: the seek of key-based paging.
    /// </summary>
    /// <param name="key">How the list sorts the cursor's user.</param>
    /// <param name="column">What the list is sorted by.</param>
    /// <param name="direction">Which way the page is read.</param>
    /// <returns>The condition.</returns>
    internal static Expression<Func<User, bool>> Beyond(
        UserSortKey key,
        UserSortColumn column,
        SortDirection direction)
    {
        return direction == SortDirection.Descending
            ? Earlier(key, column)
            : Later(key, column);
    }

    /// <summary>
    /// Keeps the users who come after the key when the list is read smallest first: a greater value in the sorted
    /// column, or the same value and a greater identifier.
    /// </summary>
    /// <param name="key">How the list sorts the cursor's user.</param>
    /// <param name="column">What the list is sorted by.</param>
    /// <returns>The condition.</returns>
    private static Expression<Func<User, bool>> Later(UserSortKey key, UserSortColumn column)
    {
        UserId id = key.Id;
        DisplayName name = key.DisplayName;
        Username username = key.Username;
        EmailAddress email = key.Email;
        DateTime changed = key.LastModifiedOn;
        return column switch
        {
            UserSortColumn.Username =>
                user => user.Username.IsGreaterThanOrEqualTo(username)
                    && (user.Username.IsGreaterThan(username) || user.Id.IsGreaterThan(id)),
            UserSortColumn.Email =>
                user => user.Email.IsGreaterThanOrEqualTo(email)
                    && (user.Email.IsGreaterThan(email) || user.Id.IsGreaterThan(id)),
            UserSortColumn.Updated =>
                user => user.LastModifiedOn >= changed
                    && (user.LastModifiedOn > changed || user.Id.IsGreaterThan(id)),
            _ =>
                user => user.DisplayName.IsGreaterThanOrEqualTo(name)
                    && (user.DisplayName.IsGreaterThan(name) || user.Id.IsGreaterThan(id)),
        };
    }

    /// <summary>
    /// Keeps the users who come after the key when the list is read largest first: a smaller value in the sorted
    /// column, or the same value and a smaller identifier. The conditions are <see cref="Later"/>'s, turned round.
    /// </summary>
    /// <param name="key">How the list sorts the cursor's user.</param>
    /// <param name="column">What the list is sorted by.</param>
    /// <returns>The condition.</returns>
    private static Expression<Func<User, bool>> Earlier(UserSortKey key, UserSortColumn column)
    {
        UserId id = key.Id;
        DisplayName name = key.DisplayName;
        Username username = key.Username;
        EmailAddress email = key.Email;
        DateTime changed = key.LastModifiedOn;
        return column switch
        {
            UserSortColumn.Username =>
                user => user.Username.IsLessThanOrEqualTo(username)
                    && (user.Username.IsLessThan(username) || user.Id.IsLessThan(id)),
            UserSortColumn.Email =>
                user => user.Email.IsLessThanOrEqualTo(email)
                    && (user.Email.IsLessThan(email) || user.Id.IsLessThan(id)),
            UserSortColumn.Updated =>
                user => user.LastModifiedOn <= changed
                    && (user.LastModifiedOn < changed || user.Id.IsLessThan(id)),
            _ =>
                user => user.DisplayName.IsLessThanOrEqualTo(name)
                    && (user.DisplayName.IsLessThan(name) || user.Id.IsLessThan(id)),
        };
    }
}
