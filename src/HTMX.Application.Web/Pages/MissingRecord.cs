namespace HTMX.Application.Web.Pages;

/// <summary>
/// What a record's page shows when no record has the identifier in its route: the identifier, and whether the page's
/// heading takes the focus, as it does on every page that HTMX loads.
/// </summary>
public sealed record MissingRecord
{
    /// <summary>
    /// Gets the identifier from the route, which names no record.
    /// </summary>
    required public long Id { get; init; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the page, as by a link,
    /// since HTMX moves the keyboard nowhere itself, and not when the browser loaded it on its own.
    /// </summary>
    required public bool HeadingFocused { get; init; }
}
