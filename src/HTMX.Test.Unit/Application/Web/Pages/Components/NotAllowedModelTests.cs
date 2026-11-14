namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using System.Threading.Tasks;
using HTMX.Application.Web.Authorization;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="NotAllowedModel"/> class: a list of articles that
/// offers Publish and Unpublish only to a user who may publish, and refuses one pressed after that permission was
/// withdrawn, with HTMX and without it.
/// </summary>
[TestClass]
public sealed class NotAllowedModelTests
{
    #region Private Fields

    // What the refusal's toast says.
    private const string RefusedMessage = "You may not publish articles. The list now shows only what you may do.";

    // The policies met for a user who may publish, and for one who may not.
    private static readonly string[] Publisher = [PublishingPolicy.Name];
    private static readonly string[] NotPublisher = [];

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Unit test to verify that the page offers Publish and Unpublish only to a user the Publishing policy lets
    /// publish, and lists every article either way.
    /// </summary>
    /// <param name="may">Whether the user may publish.</param>
    [TestMethod]
    [DataRow(true, DisplayName = "A user who may publish")]
    [DataRow(false, DisplayName = "A user who may not")]
    public async Task OnGetAsync_EachUser_OffersOnlyWhatTheyMayDo(bool may)
    {
        // Arrange (Given)
        NotAllowedModel page = CreatePage(new ArticleDesk(), may ? Publisher : NotPublisher, HtmxRequest.None);

        // Act (When)
        await page.OnGetAsync().ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (may, 3, false),
            (page.MayPublish, page.Articles.Length, page.ListHeadingFocused),
            "The page should list every article, and offer the actions only to a user who may publish.");
    }

    /// <summary>
    /// Unit test to verify that the page's heading takes the keyboard only when HTMX loaded the whole page.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="focused">Whether the heading should take the keyboard.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Boosted), true, DisplayName = "A boosted link")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The browser on its own")]
    public async Task OnGetAsync_EachKindOfRequest_FocusesTheHeadingOnlyForHtmx(string kind, bool focused)
    {
        // Arrange (Given)
        NotAllowedModel page = CreatePage(new ArticleDesk(), Publisher, Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        await page.OnGetAsync().ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            focused,
            page.HeadingFocused,
            "The heading should take the keyboard only for HTMX.");
    }

    /// <summary>
    /// Unit test to verify that a user who may publish publishes an article with HTMX, and the answer is the list as it
    /// is now and a toast that says what was published.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task OnPostPublishAsync_MayPublishByHtmx_AnswersTheListAndAToast()
    {
        // Arrange (Given)
        ArticleDesk desk = new ArticleDesk();
        NotAllowedModel page = CreatePage(desk, Publisher, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostPublishAsync(10003).ConfigureAwait(false);
        Article saved = desk.All()[2];

        // Assert (Then)
        Assert.AreEqual(
            ("_ArticleAnswer", "Mock price list published.", true, true),
            ((result as PartialViewResult)?.ViewName, page.Toast?.Message, page.Articles[2].Published, saved.Published),
            "The article should be published, and HTMX get the list and the toast.");
        Assert.IsTrue(
            page.MayPublish,
            "The list should still offer the actions.");
    }

    /// <summary>
    /// Unit test to verify that an article published without HTMX is redirected back to the page, the toast left in
    /// TempData for it, so that refreshing the page does not post again.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task OnPostPublishAsync_MayPublishWithoutHtmx_RedirectsWithTheToast()
    {
        // Arrange (Given)
        NotAllowedModel page = CreatePage(new ArticleDesk(), Publisher, HtmxRequest.None);

        // Act (When)
        IActionResult result = await page.OnPostPublishAsync(10002).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "The browser should be sent back to the page.");
        Assert.AreEqual(
            "Sample spring newsletter published.",
            page.TempData["ToastMessage"],
            "The toast should wait in TempData for the page.");
    }

    /// <summary>
    /// Unit test to verify that a user who may publish makes a published article a draft again with HTMX.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task OnPostUnpublishAsync_MayPublishByHtmx_MakesItADraft()
    {
        // Arrange (Given)
        ArticleDesk desk = new ArticleDesk();
        NotAllowedModel page = CreatePage(desk, Publisher, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostUnpublishAsync(10001).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ("_ArticleAnswer", "Placeholder opening hours unpublished.", false),
            ((result as PartialViewResult)?.ViewName, page.Toast?.Message, desk.All()[0].Published),
            "The article should be a draft again, and HTMX get the list and the toast.");
    }

    /// <summary>
    /// Unit test to verify that Publish pressed by a user who may not publish, as after the permission was withdrawn,
    /// is refused with 403: the list drawn without the actions, its heading taking the keyboard, and a toast that says
    /// why, alone for HTMX and in the whole page without it; and nothing is published.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="listAlone">Whether the answer should be the list alone.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Partial), true, DisplayName = "With HTMX")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "Without HTMX")]
    public async Task OnPostPublishAsync_PermissionWithdrawn_RefusesWithTheList(string kind, bool listAlone)
    {
        // Arrange (Given)
        ArticleDesk desk = new ArticleDesk();
        NotAllowedModel page = CreatePage(desk, NotPublisher, Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        IActionResult result = await page.OnPostPublishAsync(10003).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (StatusCodes.Status403Forbidden, listAlone, !listAlone, false),
            (page.Response.StatusCode, result is PartialViewResult, result is PageResult, desk.All()[2].Published),
            "The action should be refused with the list or the page, and nothing published.");
        Assert.AreEqual(
            (false, true, 3, RefusedMessage),
            (page.MayPublish, page.ListHeadingFocused, page.Articles.Length, page.Toast?.Message),
            "The list should come without the actions, its heading focused, with the toast that says why.");
    }

    /// <summary>
    /// Unit test to verify that Unpublish pressed by a user who may not publish is refused the same way, and leaves the
    /// article published.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task OnPostUnpublishAsync_PermissionWithdrawn_Refuses()
    {
        // Arrange (Given)
        ArticleDesk desk = new ArticleDesk();
        NotAllowedModel page = CreatePage(desk, NotPublisher, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostUnpublishAsync(10001).ConfigureAwait(false);
        string? view = (result as PartialViewResult)?.ViewName;

        // Assert (Then)
        Assert.AreEqual(
            (StatusCodes.Status403Forbidden, "_ArticleAnswer", RefusedMessage, true),
            (page.Response.StatusCode, view, page.Toast?.Message, desk.All()[0].Published),
            "The action should be refused, and the article left published.");
    }

    /// <summary>
    /// Unit test to verify that an article the desk does not have answers 404 for a user who may publish.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task OnPostPublishAsync_UnknownArticle_AnswersNotFound()
    {
        // Arrange (Given)
        NotAllowedModel page = CreatePage(new ArticleDesk(), Publisher, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = await page.OnPostPublishAsync(99).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<NotFoundResult>(
            result,
            "An article the desk does not have should answer 404.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the page as the framework would for one request: the headers HTMX 4 sends for that kind of request,
    /// TempData, and an authorization service that meets the policies given.
    /// </summary>
    /// <param name="desk">The articles.</param>
    /// <param name="met">The policies the user meets.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static NotAllowedModel CreatePage(ArticleDesk desk, string[] met, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Components/NotAllowed");
        return new NotAllowedModel(desk, new StubAuthorizationService(met))
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
        };
    }

    #endregion Private Methods
}
