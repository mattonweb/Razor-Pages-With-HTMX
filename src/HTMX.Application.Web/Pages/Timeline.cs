namespace HTMX.Application.Web.Pages;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// A record's versions as its History tab shows them, newest first, with where a roll-back posts to and what its
/// answer replaces. <c>_Timeline</c> renders it, for any kind of record.
/// </summary>
public sealed record Timeline
{
    /// <summary>
    /// Gets the record's name, as a roll-back's dialog names it.
    /// </summary>
    required public string RecordName { get; init; }

    /// <summary>
    /// Gets the version the record is at now.
    /// </summary>
    required public VersionNumber Current { get; init; }

    /// <summary>
    /// Gets the versions, newest first.
    /// </summary>
    required public TimelineEntry[] Entries { get; init; }

    /// <summary>
    /// Gets the address a roll-back posts to: the History tab's RollBack handler.
    /// </summary>
    required public string RollBackAction { get; init; }

    /// <summary>
    /// Gets the element a roll-back's answer replaces when HTMX sends it, such as <c>#organization</c>.
    /// </summary>
    required public string Target { get; init; }
}
