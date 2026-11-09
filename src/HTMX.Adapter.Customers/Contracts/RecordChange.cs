namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// One change to a record: when it was made, and by whom.
/// </summary>
public sealed record RecordChange
{
    /// <summary>
    /// Gets when the change was made, in UTC.
    /// </summary>
    required public DateTime On { get; init; }

    /// <summary>
    /// Gets the user who made it.
    /// </summary>
    required public UserReference By { get; init; }
}
