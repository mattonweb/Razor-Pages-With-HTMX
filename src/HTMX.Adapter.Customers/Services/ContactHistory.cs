namespace HTMX.Adapter.Customers.Services;

using System;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// How each change to a contact becomes their next version, and how their row then takes that version's values, as the
/// seed builds every row from its latest version. <see cref="ContactService"/> decides whether a change may be made;
/// these only make the rows it saves, and need no query.
/// </summary>
internal static class ContactHistory
{
    /// <summary>
    /// Makes a new contact's row: the values sent, active, at version 1, created and last changed now by whoever sent
    /// them. The database gives the identifier as the row is saved.
    /// </summary>
    /// <param name="request">The new contact.</param>
    /// <param name="now">When they are created, in UTC.</param>
    /// <returns>The row, without an identifier.</returns>
    internal static Contact Start(ContactCreateRequest request, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new Contact
        {
            Name = request.Name,
            Email = request.Email,
            PhoneNumbers = request.PhoneNumbers,
            JobTitle = request.JobTitle,
            OrganizationId = request.Organization,
            Notes = request.Notes,
            CreatedOn = now,
            CreatedBy = request.CreatedBy,
            LastModifiedOn = now,
            LastModifiedBy = request.CreatedBy,
            Version = new VersionNumber(1),
        };
    }

    /// <summary>
    /// Makes a new contact's first version from their row as saved: a Created version holding the same values.
    /// </summary>
    /// <param name="contact">The row, with the identifier the database gave it.</param>
    /// <returns>The version.</returns>
    internal static ContactVersion RecordCreation(Contact contact)
    {
        ArgumentNullException.ThrowIfNull(contact);
        return new ContactVersion
        {
            ContactId = contact.Id,
            Version = contact.Version,
            Change = ChangeKind.Created,
            ChangedOn = contact.CreatedOn,
            ChangedBy = contact.CreatedBy,
            Name = contact.Name,
            Email = contact.Email,
            PhoneNumbers = contact.PhoneNumbers,
            JobTitle = contact.JobTitle,
            OrganizationId = contact.OrganizationId,
            Notes = contact.Notes,
        };
    }

    /// <summary>
    /// Makes the version that records a change: the next number, the values sent, active, and who saved them and
    /// when.
    /// </summary>
    /// <param name="current">The contact as they are now, who is active.</param>
    /// <param name="request">The change.</param>
    /// <param name="rolledBackTo">The earlier version the values came from, or <see langword="null"/> for an
    /// edit.</param>
    /// <param name="now">When the change is saved, in UTC.</param>
    /// <returns>The version.</returns>
    internal static ContactVersion Record(
        Contact current,
        ContactUpdateRequest request,
        VersionNumber? rolledBackTo,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(request);
        return new ContactVersion
        {
            ContactId = current.Id,
            Version = new VersionNumber(current.Version.Value + 1),
            Change = rolledBackTo is null ? ChangeKind.Edited : ChangeKind.RolledBack,
            RolledBackTo = rolledBackTo,
            ChangedOn = now,
            ChangedBy = request.ChangedBy,
            Name = request.Name,
            Email = request.Email,
            PhoneNumbers = request.PhoneNumbers,
            JobTitle = request.JobTitle,
            OrganizationId = request.Organization,
            Notes = request.Notes,
        };
    }

    /// <summary>
    /// Makes the version that records a contact being archived or restored: the next number, their values as they
    /// are, and when and by whom they were archived, or nothing there once they are restored.
    /// </summary>
    /// <param name="current">The contact as they are now.</param>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="by">Who is archiving or restoring them.</param>
    /// <param name="now">When, in UTC.</param>
    /// <returns>The version.</returns>
    internal static ContactVersion RecordStatusChange(Contact current, ChangeKind change, UserId by, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(current);
        bool archiving = change == ChangeKind.Archived;
        return new ContactVersion
        {
            ContactId = current.Id,
            Version = new VersionNumber(current.Version.Value + 1),
            Change = change,
            ChangedOn = now,
            ChangedBy = by,
            Name = current.Name,
            Email = current.Email,
            PhoneNumbers = current.PhoneNumbers,
            JobTitle = current.JobTitle,
            OrganizationId = current.OrganizationId,
            Notes = current.Notes,
            ArchivedOn = archiving ? now : null,
            ArchivedBy = archiving ? by : null,
        };
    }

    /// <summary>
    /// Gives a contact's row the values of their newest version, keeping who created them and when.
    /// </summary>
    /// <param name="current">The row as it is now.</param>
    /// <param name="version">The newest version.</param>
    /// <returns>The row's new values.</returns>
    internal static Contact Apply(Contact current, ContactVersion version)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(version);
        return current with
        {
            Name = version.Name,
            Email = version.Email,
            PhoneNumbers = version.PhoneNumbers,
            JobTitle = version.JobTitle,
            OrganizationId = version.OrganizationId,
            Notes = version.Notes,
            LastModifiedOn = version.ChangedOn,
            LastModifiedBy = version.ChangedBy,
            ArchivedOn = version.ArchivedOn,
            ArchivedBy = version.ArchivedBy,
            Version = version.Version,
        };
    }
}
