namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="StockTableModel"/> class: a table whose rows each
/// save how many of an item are on hand, with HTMX and without it.
/// </summary>
[TestClass]
public sealed class StockTableModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a count HTMX sent is saved, and the answer is the row as saved and a toast that says
    /// how many are on hand.
    /// </summary>
    [TestMethod]
    public void OnPostSave_CountedByHtmx_AnswersTheRowAndAToast()
    {
        // Arrange (Given)
        StockShelf shelf = new StockShelf();
        StockTableModel page = CreatePage(shelf, HtmxRequest.Partial);
        page.Input = new StockInput { OnHand = 15 };

        // Act (When)
        IActionResult result = page.OnPostSave(10003);

        // Assert (Then)
        Assert.AreEqual(
            ("_StockSaved", "15", "Mock mugs: 15 on hand.", 15),
            ((result as PartialViewResult)?.ViewName, page.Row?.Value, page.Toast?.Message, shelf.Find(10003)?.OnHand),
            "The count should be saved, and HTMX get the row and the toast.");
    }

    /// <summary>
    /// Unit test to verify that a count sent without HTMX is saved and redirected back to the page, the toast left in
    /// TempData for it, so that refreshing the page does not post again.
    /// </summary>
    [TestMethod]
    public void OnPostSave_CountedWithoutHtmx_RedirectsWithTheToast()
    {
        // Arrange (Given)
        StockTableModel page = CreatePage(new StockShelf(), HtmxRequest.None);
        page.Input = new StockInput { OnHand = 1 };

        // Act (When)
        IActionResult result = page.OnPostSave(10004);

        // Assert (Then)
        Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "The browser should be sent back to the page.");
        Assert.AreEqual(
            "Dummy desk lamps: 1 on hand.",
            page.TempData["ToastMessage"],
            "The toast should wait in TempData for the page.");
    }

    /// <summary>
    /// Unit test to verify that a refused count answers 422 with its row showing what was posted and the message,
    /// alone for HTMX and among the other rows, as the shelf holds them, without it; and saves nothing.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="rowAlone">Whether the answer should be the row alone.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Partial), true, DisplayName = "With HTMX")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "Without HTMX")]
    public void OnPostSave_Refused_MarksItsRow(string kind, bool rowAlone)
    {
        // Arrange (Given)
        StockShelf shelf = new StockShelf();
        StockTableModel page = CreatePage(shelf, Enum.Parse<HtmxRequest>(kind));
        page.ModelState.SetModelValue("Input.OnHand", "1500", "1500");
        page.ModelState.AddModelError("Input.OnHand", "Enter a count from 0 to 999.");

        // Act (When)
        IActionResult result = page.OnPostSave(10002);
        StockRow refused = page.Rows[1];

        // Assert (Then)
        Assert.AreEqual(
            (StatusCodes.Status422UnprocessableEntity, rowAlone, !rowAlone, 40),
            (page.Response.StatusCode, result is PartialViewResult, result is PageResult, shelf.Find(10002)?.OnHand),
            "The count should be refused with the row or the page, and nothing saved.");
        Assert.AreEqual(
            ("1500", "Enter a count from 0 to 999.", "120", null),
            (refused.Value, refused.Message, page.Rows[0].Value, page.Rows[0].Message),
            "Only the refused row should show what was posted and the message.");
    }

    /// <summary>
    /// Unit test to verify that a count posted for an item the shelf does not have answers 404.
    /// </summary>
    [TestMethod]
    public void OnPostSave_UnknownItem_AnswersNotFound()
    {
        // Arrange (Given)
        StockTableModel page = CreatePage(new StockShelf(), HtmxRequest.Partial);
        page.Input = new StockInput { OnHand = 5 };

        // Act (When)
        IActionResult result = page.OnPostSave(99);

        // Assert (Then)
        Assert.IsInstanceOfType<NotFoundResult>(
            result,
            "An item the shelf does not have should answer 404.");
    }

    /// <summary>
    /// Unit test to verify that the page draws a row for every item, its field showing the count on hand, and that its
    /// heading takes the keyboard only when HTMX loaded the whole page.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="focused">Whether the heading should take the keyboard.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Boosted), true, DisplayName = "A boosted link")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The browser on its own")]
    public void OnGet_EachKindOfRequest_DrawsEveryRow(string kind, bool focused)
    {
        // Arrange (Given)
        StockTableModel page = CreatePage(new StockShelf(), Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        page.OnGet();

        // Assert (Then)
        Assert.AreEqual(
            (focused, 4, "120", "3"),
            (page.HeadingFocused, page.Rows.Length, page.Rows[0].Value, page.Rows[3].Value),
            "The page should draw every item's row, the heading focused only for HTMX.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the page as the framework would for one request: the headers HTMX 4 sends for that kind of request,
    /// TempData, and what a partial needs to render.
    /// </summary>
    /// <param name="shelf">The items.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static StockTableModel CreatePage(StockShelf shelf, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Components/StockTable");
        return new StockTableModel(shelf)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
        };
    }

    #endregion Private Methods
}
