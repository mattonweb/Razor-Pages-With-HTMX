namespace HTMX.Application.Web.Pages.Components.Card;

using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Card page, at /components/card: the card and each of its parts, each drawn by <see cref="CardTagHelper"/>,
/// which sits beside this page, so that the component is one folder.
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
