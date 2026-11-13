namespace HTMX.Application.Web.Pages;

using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The home page, at /: what the application is and where to start. It is named HomeModel, not IndexModel, so that
/// it is never mistaken for the organizations' or the users' list, whose models have that name.
/// </summary>
public sealed class HomeModel : PageModel
{
    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the page, as by the brand's
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
