namespace HTMX.Application.Web.Pages.Components;

using System;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using HTMX.Application.Web.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Publishing permission page, at /components/publishing-permission: whether the signed-in user may publish the
/// Not allowed page's articles, with a button that withdraws the permission or grants it again, as an administrator's
/// page would.
/// </summary>
/// <remarks>
/// It changes the permission and nothing else. The Not allowed page, open in another tab, keeps what it drew until it
/// is drawn again, which is how a page comes to offer what its user may no longer do.
/// </remarks>
public sealed class PublishingPermissionModel : PageModel
{
    // What a change answers HTMX with: the permission's card, which the page draws too, and a toast.
    private const string ChangedPartial = "_PermissionChanged";

    // The element a toast's Undo would replace; no toast here offers one.
    private const string CardTarget = "#permission";

    private readonly Publishers _publishers;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublishingPermissionModel"/> class.
    /// </summary>
    /// <param name="publishers">Who may publish.</param>
    public PublishingPermissionModel(Publishers publishers)
    {
        ArgumentNullException.ThrowIfNull(publishers);
        this._publishers = publishers;
    }

    /// <summary>
    /// Gets a value indicating whether the signed-in user may publish.
    /// </summary>
    public bool MayPublish { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Gets the toast that says what changed, or <see langword="null"/>.
    /// </summary>
    public Toast? Toast { get; private set; }

    /// <summary>
    /// Shows whether the signed-in user may publish, and the toast a change without HTMX left before its redirect.
    /// </summary>
    public void OnGet()
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        this.Toast = TempDataToasts.Take(this.TempData, CardTarget);
        this.MayPublish = this._publishers.May(SignedInUser.Of(this.User));
    }

    /// <summary>
    /// Stops the signed-in user publishing.
    /// </summary>
    /// <returns>The card and a toast for HTMX, or the redirect back to the page.</returns>
    public IActionResult OnPostWithdraw()
    {
        UserId user = SignedInUser.Of(this.User);
        this._publishers.Withdraw(user);
        return this.Changed(user, "You may no longer publish articles.");
    }

    /// <summary>
    /// Lets the signed-in user publish again.
    /// </summary>
    /// <returns>The card and a toast for HTMX, or the redirect back to the page.</returns>
    public IActionResult OnPostGrant()
    {
        UserId user = SignedInUser.Of(this.User);
        this._publishers.Grant(user);
        return this.Changed(user, "You may publish articles again.");
    }

    /// <summary>
    /// Answers a change: the card as it is now and a toast for HTMX, and, without it, a redirect back to the page with
    /// the toast left in TempData, so that a refresh does not post again.
    /// </summary>
    /// <param name="user">The user whose permission changed.</param>
    /// <param name="message">What changed.</param>
    /// <returns>The card and the toast, or the redirect.</returns>
    private IActionResult Changed(UserId user, string message)
    {
        Toast toast = new Toast { Message = message };
        if (!HtmxHeaders.IsPartialRequest(this.Request))
        {
            TempDataToasts.Leave(this.TempData, toast);
            return this.RedirectToPage();
        }

        this.MayPublish = this._publishers.May(user);
        this.Toast = toast;
        return this.Partial(ChangedPartial, this);
    }
}
