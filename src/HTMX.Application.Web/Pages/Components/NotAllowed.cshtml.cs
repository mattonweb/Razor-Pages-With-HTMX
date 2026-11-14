namespace HTMX.Application.Web.Pages.Components;

using System;
using System.Threading.Tasks;
using HTMX.Application.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Not allowed page, at /components/not-allowed: a list of articles, each with Publish or Unpublish for a user who
/// may publish, and what an action pressed after that permission was withdrawn answers.
/// </summary>
/// <remarks>
/// Every signed-in user may see the page. Publishing needs the Publishing policy, which the page asks as it draws the
/// list, so that the list offers only what the user may do, and again as each action runs, since the permission can be
/// withdrawn after the list was drawn. Such an action is refused here, in the handler, rather than with
/// <c>Forbid()</c>: that hands the request to the sign-in, whose answer is made for a whole window, such as a cookie
/// sign-in's redirect to its access-denied page, and leaves the list offering what was refused. The refusal is 403 with
/// the list drawn as the user may now use it, and a toast that says why.
/// </remarks>
public sealed class NotAllowedModel : PageModel
{
    // What an action answers HTMX with: the list's card, which the page draws too, and a toast.
    private const string AnswerPartial = "_ArticleAnswer";

    // The element a toast's Undo would replace; no toast here offers one.
    private const string ListTarget = "#articles";

    private readonly ArticleDesk _desk;
    private readonly IAuthorizationService _authorization;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotAllowedModel"/> class.
    /// </summary>
    /// <param name="desk">The articles, which the list shows and its actions publish.</param>
    /// <param name="authorization">Asks the Publishing policy whether the request's user may publish.</param>
    public NotAllowedModel(ArticleDesk desk, IAuthorizationService authorization)
    {
        ArgumentNullException.ThrowIfNull(desk);
        ArgumentNullException.ThrowIfNull(authorization);
        this._desk = desk;
        this._authorization = authorization;
    }

    /// <summary>
    /// Gets the articles the list shows, in order.
    /// </summary>
    public Article[] Articles { get; private set; } = [];

    /// <summary>
    /// Gets a value indicating whether the user may publish, so that the list offers Publish and Unpublish.
    /// </summary>
    public bool MayPublish { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the list's heading takes the focus: when an action was refused, since the button
    /// pressed is no longer drawn.
    /// </summary>
    public bool ListHeadingFocused { get; private set; }

    /// <summary>
    /// Gets the toast that says what an action did, or why it was refused, or <see langword="null"/>.
    /// </summary>
    public Toast? Toast { get; private set; }

    /// <summary>
    /// Shows the list, with Publish and Unpublish when the user may publish, and the toast an action without HTMX left
    /// before its redirect.
    /// </summary>
    /// <returns>A task that completes when the page's model is ready.</returns>
    public async Task OnGetAsync()
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        this.Toast = TempDataToasts.Take(this.TempData, ListTarget);
        this.MayPublish = await this.MayPublishAsync().ConfigureAwait(false);
        this.Articles = this._desk.All();
    }

    /// <summary>
    /// Publishes an article, when the user may publish.
    /// </summary>
    /// <param name="article">The article's number, which its row's form names in its address.</param>
    /// <returns>The list and a toast, the redirect back to the page, the refusal, or 404 for an article the desk does
    /// not have.</returns>
    public async Task<IActionResult> OnPostPublishAsync(long article)
    {
        if (!await this.MayPublishAsync().ConfigureAwait(false))
        {
            return this.Refuse();
        }

        Article? published = this._desk.Publish(article);
        return published is null ? this.NotFound() : this.Changed($"{published.Title} published.");
    }

    /// <summary>
    /// Makes an article a draft again, when the user may publish.
    /// </summary>
    /// <param name="article">The article's number, which its row's form names in its address.</param>
    /// <returns>The list and a toast, the redirect back to the page, the refusal, or 404 for an article the desk does
    /// not have.</returns>
    public async Task<IActionResult> OnPostUnpublishAsync(long article)
    {
        if (!await this.MayPublishAsync().ConfigureAwait(false))
        {
            return this.Refuse();
        }

        Article? unpublished = this._desk.Unpublish(article);
        return unpublished is null ? this.NotFound() : this.Changed($"{unpublished.Title} unpublished.");
    }

    /// <summary>
    /// Asks the Publishing policy whether the request's user may publish.
    /// </summary>
    /// <returns><see langword="true"/> when the user may.</returns>
    private async Task<bool> MayPublishAsync()
    {
        AuthorizationResult result = await this._authorization.AuthorizeAsync(this.User, PublishingPolicy.Name)
            .ConfigureAwait(false);
        return result.Succeeded;
    }

    /// <summary>
    /// Answers an action that ran: the list as it is now and a toast for HTMX, and, without it, a redirect back to the
    /// page with the toast left in TempData, so that a refresh does not post again.
    /// </summary>
    /// <param name="message">What the action did.</param>
    /// <returns>The list and the toast, or the redirect.</returns>
    private IActionResult Changed(string message)
    {
        Toast toast = new Toast { Message = message };
        if (!HtmxHeaders.IsPartialRequest(this.Request))
        {
            TempDataToasts.Leave(this.TempData, toast);
            return this.RedirectToPage();
        }

        this.MayPublish = true;
        this.Articles = this._desk.All();
        this.Toast = toast;
        return this.Partial(AnswerPartial, this);
    }

    /// <summary>
    /// Refuses an action the user may not take, with 403: the list drawn without Publish and Unpublish, its heading
    /// taking the keyboard, and a toast that says why; the list alone for HTMX, and the whole page otherwise.
    /// </summary>
    /// <returns>The list and the toast, or the page.</returns>
    private IActionResult Refuse()
    {
        this.Response.StatusCode = StatusCodes.Status403Forbidden;
        this.MayPublish = false;
        this.ListHeadingFocused = true;
        this.Articles = this._desk.All();
        this.Toast = new Toast { Message = "You may not publish articles. The list now shows only what you may do." };
        return HtmxHeaders.IsPartialRequest(this.Request) ? this.Partial(AnswerPartial, this) : this.Page();
    }
}
