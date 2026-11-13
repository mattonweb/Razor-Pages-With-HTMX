namespace HTMX.Test.Integration;

/// <summary>
/// The kinds of request a test sends, each with the headers HTMX 4.0.1 sends for it.
/// </summary>
internal enum HtmxRequest
{
    /// <summary>
    /// The browser itself, without HTMX: no HTMX headers.
    /// </summary>
    None,

    /// <summary>
    /// HTMX swapping part of the page: HX-Request is sent, and HX-Request-Type is partial.
    /// </summary>
    Partial,

    /// <summary>
    /// HTMX loading the whole page for a boosted link: HX-Request and HX-Boosted are sent, and HX-Request-Type is full.
    /// </summary>
    Boosted,

    /// <summary>
    /// HTMX restoring a page for the back button: HX-Request-Type is full and HX-History-Restore-Request is sent, but
    /// HX-Request is not.
    /// </summary>
    HistoryRestore,
}
