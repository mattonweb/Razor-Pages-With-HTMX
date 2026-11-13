namespace HTMX.Application.Web.Pages.Components;

/// <summary>
/// One note on the Reload on event page's board: its number, in the order the notes were written, and its text.
/// </summary>
public sealed record Note
{
    /// <summary>
    /// Gets the note's number, from 10001.
    /// </summary>
    required public long Number { get; init; }

    /// <summary>
    /// Gets the note's text, as written.
    /// </summary>
    required public string Text { get; init; }
}
