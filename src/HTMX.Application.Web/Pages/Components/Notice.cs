namespace HTMX.Application.Web.Pages.Components;

/// <summary>
/// One notice on the Seen notices page's board: its number, its text, and whether it has been seen.
/// </summary>
public sealed record Notice
{
    /// <summary>
    /// Gets the notice's number, from 10001, which its request to be marked seen names.
    /// </summary>
    required public long Number { get; init; }

    /// <summary>
    /// Gets the notice's text.
    /// </summary>
    required public string Text { get; init; }

    /// <summary>
    /// Gets a value indicating whether the notice has been seen.
    /// </summary>
    public bool Seen { get; init; }
}
