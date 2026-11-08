namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// Which side of its cursor's record a page of a list lies on.
/// </summary>
public enum CursorPosition
{
    /// <summary>
    /// The page starts with the record that follows the cursor's: where a Next link goes.
    /// </summary>
    After,

    /// <summary>
    /// The page ends with the record that comes before the cursor's: where a Previous link goes.
    /// </summary>
    Before,
}
