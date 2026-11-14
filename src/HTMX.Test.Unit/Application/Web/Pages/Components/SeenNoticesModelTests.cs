namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="SeenNoticesModel"/> class: notices that mark
/// themselves seen as they come into view, and buttons that mark them all.
/// </summary>
[TestClass]
public sealed class SeenNoticesModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a notice HTMX marks seen as it comes into view is marked, and the answer, 204, has
    /// nothing to draw.
    /// </summary>
    [TestMethod]
    public void OnPostSeen_KnownNotice_MarksItAndAnswersNoContent()
    {
        // Arrange (Given)
        NoticeBoard board = new NoticeBoard();
        SeenNoticesModel page = CreatePage(board, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = page.OnPostSeen(10003);

        // Assert (Then)
        Assert.AreEqual(
            (StatusCodes.Status204NoContent, true),
            ((result as StatusCodeResult)?.StatusCode, board.All()[2].Seen),
            "The notice should be seen, with nothing to draw.");
    }

    /// <summary>
    /// Unit test to verify that a notice the board does not have answers 404.
    /// </summary>
    [TestMethod]
    public void OnPostSeen_UnknownNotice_AnswersNotFound()
    {
        // Arrange (Given)
        SeenNoticesModel page = CreatePage(new NoticeBoard(), HtmxRequest.Partial);

        // Act (When)
        IActionResult result = page.OnPostSeen(99);

        // Assert (Then)
        Assert.IsInstanceOfType<NotFoundResult>(
            result,
            "A notice the board does not have should answer 404.");
    }

    /// <summary>
    /// Unit test to verify that Mark all as seen marks every notice, and answers HTMX with the list drawn again and the
    /// browser on its own with a redirect back to the page.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="listAlone">Whether the answer should be the list alone.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Partial), true, DisplayName = "With HTMX")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "Without HTMX")]
    public void OnPostAllSeen_EachKindOfRequest_MarksEveryNotice(string kind, bool listAlone)
    {
        // Arrange (Given)
        NoticeBoard board = new NoticeBoard();
        SeenNoticesModel page = CreatePage(board, Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        IActionResult result = page.OnPostAllSeen();

        // Assert (Then)
        Assert.AreEqual(
            (listAlone ? "_NoticeList" : null, !listAlone, true),
            ((result as PartialViewResult)?.ViewName, result is RedirectToPageResult, board.All()[15].Seen),
            "Every notice should be seen, and the answer be the list or the redirect.");
    }

    /// <summary>
    /// Unit test to verify that Mark all as new marks every notice new again, and the list HTMX gets shows them so.
    /// </summary>
    [TestMethod]
    public void OnPostAllNew_ByHtmx_DrawsEveryNoticeNew()
    {
        // Arrange (Given)
        NoticeBoard board = new NoticeBoard();
        board.MarkAll(seen: true);
        SeenNoticesModel page = CreatePage(board, HtmxRequest.Partial);

        // Act (When)
        page.OnPostAllNew();

        // Assert (Then)
        Assert.IsTrue(
            Array.TrueForAll(page.Notices, static notice => !notice.Seen),
            "The list should show every notice new.");
    }

    /// <summary>
    /// Unit test to verify that the page shows every notice, and that its heading takes the keyboard only when HTMX
    /// loaded the whole page.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="focused">Whether the heading should take the keyboard.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Boosted), true, DisplayName = "A boosted link")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The browser on its own")]
    public void OnGet_EachKindOfRequest_ShowsEveryNotice(string kind, bool focused)
    {
        // Arrange (Given)
        SeenNoticesModel page = CreatePage(new NoticeBoard(), Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        page.OnGet();

        // Assert (Then)
        Assert.AreEqual(
            (focused, 16),
            (page.HeadingFocused, page.Notices.Length),
            "The page should show every notice, the heading focused only for HTMX.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the page as the framework would for one request: the headers HTMX 4 sends for that kind of request, and
    /// what a partial needs to render.
    /// </summary>
    /// <param name="board">The notices.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static SeenNoticesModel CreatePage(NoticeBoard board, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Components/SeenNotices");
        return new SeenNoticesModel(board)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
        };
    }

    #endregion Private Methods
}
