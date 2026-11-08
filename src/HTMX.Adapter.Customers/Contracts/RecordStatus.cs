namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Which records a list shows, by whether they are archived.
/// </summary>
public enum RecordStatus
{
    /// <summary>
    /// Only records that are not archived.
    /// </summary>
    Active,

    /// <summary>
    /// Only archived records.
    /// </summary>
    Archived,

    /// <summary>
    /// Every record, archived or not.
    /// </summary>
    All,
}
