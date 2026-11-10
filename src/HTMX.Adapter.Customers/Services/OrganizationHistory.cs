namespace HTMX.Adapter.Customers.Services;

using System;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Adapter.Customers.Entities;

/// <summary>
/// How each change to an organization becomes its next version, and how its row then takes that version's values, as
/// the seed builds every row from its latest version. <see cref="OrganizationService"/> decides whether a change may
/// be made; these only make the rows it saves, and need no query.
/// </summary>
internal static class OrganizationHistory
{
    /// <summary>
    /// Makes a new organization's row: the values sent, at version 1, created and last changed by whoever sent them.
    /// </summary>
    /// <param name="request">The new organization.</param>
    /// <param name="now">When it is created, in UTC.</param>
    /// <returns>The row, without an identifier until the database gives it one.</returns>
    internal static Organization Start(OrganizationCreateRequest request, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new Organization
        {
            Name = request.Name,
            Industry = request.Industry,
            Website = request.Website,
            CreatedOn = now,
            CreatedBy = request.CreatedBy,
            LastModifiedOn = now,
            LastModifiedBy = request.CreatedBy,
            Version = new VersionNumber(1),
        };
    }

    /// <summary>
    /// Makes the first version of a new organization from its row as saved: a Created version holding the same values.
    /// </summary>
    /// <param name="organization">The row, with the identifier the database gave it.</param>
    /// <returns>The version.</returns>
    internal static OrganizationVersion RecordCreation(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);
        return new OrganizationVersion
        {
            OrganizationId = organization.Id,
            Version = organization.Version,
            Change = ChangeKind.Created,
            ChangedOn = organization.CreatedOn,
            ChangedBy = organization.CreatedBy,
            Name = organization.Name,
            Industry = organization.Industry,
            Website = organization.Website,
        };
    }

    /// <summary>
    /// Makes the version that records a change: the next number, the values sent, and who saved them and when.
    /// </summary>
    /// <param name="current">The organization as it is now, which is active.</param>
    /// <param name="request">The change.</param>
    /// <param name="rolledBackTo">The earlier version the values came from, or <see langword="null"/> for an
    /// edit.</param>
    /// <param name="now">When the change is saved, in UTC.</param>
    /// <returns>The version.</returns>
    internal static OrganizationVersion Record(
        Organization current,
        OrganizationUpdateRequest request,
        VersionNumber? rolledBackTo,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(request);
        return new OrganizationVersion
        {
            OrganizationId = current.Id,
            Version = new VersionNumber(current.Version.Value + 1),
            Change = rolledBackTo is null ? ChangeKind.Edited : ChangeKind.RolledBack,
            RolledBackTo = rolledBackTo,
            ChangedOn = now,
            ChangedBy = request.ChangedBy,
            Name = request.Name,
            Industry = request.Industry,
            Website = request.Website,
        };
    }

    /// <summary>
    /// Makes the version that records an organization being archived or restored: the next number, its values as they
    /// are, and when and by whom it was archived, or nothing there once it is restored.
    /// </summary>
    /// <param name="current">The organization as it is now.</param>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="by">Who is archiving or restoring it.</param>
    /// <param name="now">When, in UTC.</param>
    /// <returns>The version.</returns>
    internal static OrganizationVersion RecordStatusChange(
        Organization current,
        ChangeKind change,
        UserId by,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(current);
        bool archiving = change == ChangeKind.Archived;
        return new OrganizationVersion
        {
            OrganizationId = current.Id,
            Version = new VersionNumber(current.Version.Value + 1),
            Change = change,
            ChangedOn = now,
            ChangedBy = by,
            Name = current.Name,
            Industry = current.Industry,
            Website = current.Website,
            ArchivedOn = archiving ? now : null,
            ArchivedBy = archiving ? by : null,
        };
    }

    /// <summary>
    /// Gives an organization's row the values of its newest version, keeping who created it and when.
    /// </summary>
    /// <param name="current">The row as it is now.</param>
    /// <param name="version">The newest version.</param>
    /// <returns>The row's new values.</returns>
    internal static Organization Apply(Organization current, OrganizationVersion version)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(version);
        return current with
        {
            Name = version.Name,
            Industry = version.Industry,
            Website = version.Website,
            LastModifiedOn = version.ChangedOn,
            LastModifiedBy = version.ChangedBy,
            ArchivedOn = version.ArchivedOn,
            ArchivedBy = version.ArchivedBy,
            Version = version.Version,
        };
    }
}
