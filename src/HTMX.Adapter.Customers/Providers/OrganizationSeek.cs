namespace HTMX.Adapter.Customers.Providers;

using System;
using System.Linq;
using System.Linq.Expressions;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Data.Conversions;
using HTMX.Adapter.Customers.DTOs;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// The conditions that seek a page in key-based paging: those organizations that come after a cursor's, in the order
/// the page is read. <see cref="OrganizationProvider"/> adds one to the query for any page that has a cursor.
/// </summary>
internal static class OrganizationSeek
{
    /// <summary>
    /// Keeps the organizations that come after the cursor's in the order the page is read: the seek of key-based
    /// paging.
    /// </summary>
    /// <param name="key">How the list sorts the cursor's organization.</param>
    /// <param name="contacts">Every contact, which the Contacts column counts.</param>
    /// <param name="column">What the list is sorted by.</param>
    /// <param name="direction">Which way the page is read.</param>
    /// <returns>The condition.</returns>
    internal static Expression<Func<Organization, bool>> Beyond(
        OrganizationSortKey key,
        IQueryable<Contact> contacts,
        OrganizationSortColumn column,
        SortDirection direction)
    {
        return direction == SortDirection.Descending
            ? Earlier(key, contacts, column)
            : Later(key, contacts, column);
    }

    /// <summary>
    /// Keeps the organizations that come after the key when the list is read smallest first: a greater value in the
    /// sorted column, or the same value and a greater identifier.
    /// </summary>
    /// <remarks>
    /// Each condition says "at least the key's value, and then past the key", rather than "a greater value, or the same
    /// value and a greater identifier". The first half alone is a range that the index on the column and the
    /// identifier can seek to; the second half only settles the ties at its edge. LINQ cannot compare two columns as a
    /// pair, so the pair is written out. Names and identifiers are compared with the adapter's own operators,
    /// <see cref="OrganizationNameComparison"/> and <see cref="OrganizationIdComparison"/>, which Entity Framework
    /// turns into SQL on the bare columns; a cast to the stored type would become a SQL CAST, and the database would
    /// read the index from its start instead of seeking. Contacts is a count worked out for every organization, which
    /// no index can hold: its condition is right, but the database reads every organization to answer it.
    /// </remarks>
    /// <param name="key">How the list sorts the cursor's organization.</param>
    /// <param name="contacts">Every contact, which the Contacts column counts.</param>
    /// <param name="column">What the list is sorted by.</param>
    /// <returns>The condition.</returns>
    private static Expression<Func<Organization, bool>> Later(
        OrganizationSortKey key,
        IQueryable<Contact> contacts,
        OrganizationSortColumn column)
    {
        OrganizationId id = key.Id;
        OrganizationName name = key.Name;
        Industry industry = key.Industry;
        int contactCount = key.Contacts.Value;
        DateTime changed = key.LastModifiedOn;
        return column switch
        {
            OrganizationSortColumn.Industry =>
                organization => organization.Industry >= industry
                    && (organization.Industry > industry || organization.Id > id),
            OrganizationSortColumn.Contacts =>
                organization => contacts.Count(contact => contact.OrganizationId == organization.Id) >= contactCount
                    && (contacts.Count(contact => contact.OrganizationId == organization.Id) > contactCount
                        || organization.Id > id),
            OrganizationSortColumn.Updated =>
                organization => organization.LastModifiedOn >= changed
                    && (organization.LastModifiedOn > changed || organization.Id > id),
            _ =>
                organization => organization.Name >= name
                    && (organization.Name > name || organization.Id > id),
        };
    }

    /// <summary>
    /// Keeps the organizations that come after the key when the list is read largest first: a smaller value in the
    /// sorted column, or the same value and a smaller identifier. The conditions are <see cref="Later"/>'s, turned
    /// round.
    /// </summary>
    /// <param name="key">How the list sorts the cursor's organization.</param>
    /// <param name="contacts">Every contact, which the Contacts column counts.</param>
    /// <param name="column">What the list is sorted by.</param>
    /// <returns>The condition.</returns>
    private static Expression<Func<Organization, bool>> Earlier(
        OrganizationSortKey key,
        IQueryable<Contact> contacts,
        OrganizationSortColumn column)
    {
        OrganizationId id = key.Id;
        OrganizationName name = key.Name;
        Industry industry = key.Industry;
        int contactCount = key.Contacts.Value;
        DateTime changed = key.LastModifiedOn;
        return column switch
        {
            OrganizationSortColumn.Industry =>
                organization => organization.Industry <= industry
                    && (organization.Industry < industry || organization.Id < id),
            OrganizationSortColumn.Contacts =>
                organization => contacts.Count(contact => contact.OrganizationId == organization.Id) <= contactCount
                    && (contacts.Count(contact => contact.OrganizationId == organization.Id) < contactCount
                        || organization.Id < id),
            OrganizationSortColumn.Updated =>
                organization => organization.LastModifiedOn <= changed
                    && (organization.LastModifiedOn < changed || organization.Id < id),
            _ =>
                organization => organization.Name <= name
                    && (organization.Name < name || organization.Id < id),
        };
    }
}
