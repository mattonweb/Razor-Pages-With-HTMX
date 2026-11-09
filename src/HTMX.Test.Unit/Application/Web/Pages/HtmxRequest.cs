namespace HTMX.Test.Unit.Application.Web.Pages;

/// <summary>
/// The kinds of request a page test sends, each with the headers HTMX 4 sends for it.
/// </summary>
internal enum HtmxRequest
{
    /// <summary>
    /// The browser itself, without HTMX: no HTMX headers.
    /// </summary>
    None,

    /// <summary>
    /// HTMX swapping part of the page: HX-Request-Type is partial.
    /// </summary>
    Partial,

    /// <summary>
    /// HTMX restoring a page for the back button: HX-Request-Type is full, and HX-History-Restore-Request is sent.
    /// </summary>
    HistoryRestore,
}
