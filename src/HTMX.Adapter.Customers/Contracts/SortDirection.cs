namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Which way a list is sorted.
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// A to Z, smallest first, oldest first.
    /// </summary>
    Ascending,

    /// <summary>
    /// Z to A, largest first, newest first.
    /// </summary>
    Descending,
}
