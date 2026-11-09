namespace HTMX.Application.Web.Pages;

/// <summary>
/// A short message about an action that just ran, shown in the layout's toast region, with Undo when the action can be
/// undone.
/// </summary>
public sealed record Toast
{
    /// <summary>
    /// Gets the message, such as <c>Placeholder Logistics archived.</c>, which ends with a full stop.
    /// </summary>
    required public string Message { get; init; }

    /// <summary>
    /// Gets the address Undo posts to, or <see langword="null"/> when the action cannot be undone from here.
    /// </summary>
    public string? UndoAction { get; init; }

    /// <summary>
    /// Gets the element an Undo that HTMX sends replaces, such as <c>#organization</c>.
    /// </summary>
    public string? UndoTarget { get; init; }
}
