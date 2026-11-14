namespace HTMX.Test.Integration;

/// <summary>
/// How much script a browser test's page runs: every page works without HTMX, so a test can try it either way.
/// </summary>
internal enum Scripting
{
    /// <summary>
    /// Every script the page loads, HTMX's included, as a visitor's browser runs it.
    /// </summary>
    On,

    /// <summary>
    /// The application's own script but not HTMX's, as when HTMX failed to load: its file comes back empty.
    /// </summary>
    WithoutHtmx,

    /// <summary>
    /// No script at all, as in a browser with script turned off, so <c>@media (scripting: none)</c> applies.
    /// </summary>
    Off,
}
