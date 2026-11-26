namespace HTMX.Application.Web.Pages.Components.Chip;

using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Chip page, at /components/chip: every tone of the chip, each drawn by <see cref="ChipTagHelper"/>, which sits
/// beside this page, so that the component is one folder.
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
