namespace HTMX.Application.Web.Pages;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// One version of a record as its History tab compares it with the others, whatever the record: what happened, who
/// did it and when, whether it left the record archived, and the values a person edits, in the order the Details tab
/// lists them. Each record's changes class, such as <see cref="Organizations.OrganizationChanges"/>, makes these from
/// its own versions, and <see cref="RecordChanges"/> compares them.
/// </summary>
public sealed record HistoryVersion
{
    /// <summary>
    /// Gets the version's number.
    /// </summary>
    required public VersionNumber Version { get; init; }

    /// <summary>
    /// Gets what happened.
    /// </summary>
    required public ChangeKind Change { get; init; }

    /// <summary>
    /// Gets the version a roll-back went back to, or <see langword="null"/> for any other change.
    /// </summary>
    public VersionNumber? RolledBackTo { get; init; }

    /// <summary>
    /// Gets when it happened, and who did it.
    /// </summary>
    required public RecordChange Changed { get; init; }

    /// <summary>
    /// Gets when the version left the record archived, or <see langword="null"/> when it left it active.
    /// </summary>
    public DateTime? ArchivedOn { get; init; }

    /// <summary>
    /// Gets the values a person edits, in the Details tab's order. Every version of one record names the same fields
    /// in the same order, so two versions are compared field by field.
    /// </summary>
    required public FieldValue[] Values { get; init; }

    /// <summary>
    /// Gets why the record can't go back to this version even though its values differ, such as a contact's version
    /// whose organization takes no one in, or <see langword="null"/> when nothing but its values decides.
    /// </summary>
    public string? RollBackUnavailable { get; init; }
}
