namespace HTMX.Application.Web.Pages.Components;

using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Components section's first page, at /components: a list of pages that each show one pattern on its own, apart
/// from the records, working with HTMX and without it.
/// </summary>
public sealed class IndexModel : PageModel
{
    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the page, as by the side
    /// nav's boosted link, since HTMX moves the keyboard nowhere itself, and not when the browser loaded it on its own.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Shows the list.
    /// </summary>
    public void OnGet()
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
    }
}
