namespace HTMX.Application.Web.Pages;

/// <summary>
/// One field of a version as its History tab compares it: its name, and the value the version left in it, as the page
/// shows it.
/// </summary>
public sealed record FieldValue
{
    /// <summary>
    /// Gets the field's name, such as Website.
    /// </summary>
    required public string Field { get; init; }

    /// <summary>
    /// Gets the value, or <see langword="null"/> when there is none, which the diff shows as "Not set".
    /// </summary>
    public string? Value { get; init; }
}
