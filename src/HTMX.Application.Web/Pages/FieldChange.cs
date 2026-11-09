namespace HTMX.Application.Web.Pages;

/// <summary>
/// One field whose value changed, as a diff shows it: its name, then its value before and after.
/// </summary>
public sealed record FieldChange
{
    /// <summary>
    /// Gets the field's name, such as Website.
    /// </summary>
    required public string Field { get; init; }

    /// <summary>
    /// Gets the value before, or <see langword="null"/> when there was none, which the diff shows as "Not set".
    /// </summary>
    public string? Before { get; init; }

    /// <summary>
    /// Gets the value after, or <see langword="null"/> when there is none, which the diff shows as "Not set".
    /// </summary>
    public string? After { get; init; }
}
