namespace HTMX.Application.Web.Pages.Components;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The visitor's access-denied page, at /components/visitor-access-denied: where the visitor's sign-in sends a visitor
/// who may not see the page they asked for, with a link to sign in again and go back there.
/// </summary>
public sealed class VisitorAccessDeniedModel : PageModel
{
    /// <summary>
    /// Gets or sets the page the visitor was refused, as the sign-in's redirect named it. It is the sender's to write,
    /// so it is only passed on to the sign-in page, which goes back there only when it is an address on this site.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
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
