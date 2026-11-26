namespace HTMX.Application.Web.Pages.Components.Icon;

using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Icon page, at /components/icon: every icon in the sprite and each size, each drawn by
/// <see cref="IconTagHelper"/>, which sits beside this page, so that the component is one folder.
/// </summary>
public sealed class IndexModel : PageModel
{
    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the page, as by a boosted
    /// link, since HTMX moves the keyboard nowhere itself, and not when the browser loaded it on its own.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Shows the page.
    /// </summary>
    public void OnGet()
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
    }
}
