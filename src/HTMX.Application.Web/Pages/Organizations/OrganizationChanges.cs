namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Collections.Generic;
using System.Linq;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// An organization's side of its History tab: the values a version holds, in the order the Details tab lists them,
/// and when the organization can be rolled back. <see cref="RecordChanges"/> compares the versions, as it does every
/// record's.
/// </summary>
public static class OrganizationChanges
{
    /// <summary>
    /// Turns an organization's versions into its timeline's entries, newest first: what each version changed from the
    /// one before it and, while the organization is active, what rolling back to each earlier version would change.
    /// </summary>
    /// <param name="organization">The organization as it is now.</param>
    /// <param name="versions">Its versions, newest first.</param>
    /// <returns>The entries, in the same order.</returns>
    public static TimelineEntry[] Entries(
        OrganizationSingleResponse organization,
        IReadOnlyList<OrganizationVersionResponse> versions)
    {
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(versions);
        bool active = organization.Archived is null;
        return RecordChanges.Entries([.. versions.Select(Describe)], active);
    }

    /// <summary>
    /// Describes one version for the History tab to compare: the values a person edits, which are name, industry and
    /// website, and whether it left the organization archived.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>The version as every record's History tab compares it.</returns>
    public static HistoryVersion Describe(OrganizationVersionResponse version)
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
                new FieldValue { Field = "Name", Value = version.Name.Value },
                new FieldValue { Field = "Industry", Value = IndustryNames.Of(version.Industry) },
                new FieldValue { Field = "Website", Value = version.Website?.Value },
            ],
        };
    }
}
