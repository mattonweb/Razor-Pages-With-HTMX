namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using System.Linq;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="OrderLinesModel"/> class: a form of lines, each a
/// product and a quantity, which Add and Remove draw again numbered afresh, and which says what order it received.
/// </summary>
[TestClass]
public sealed class OrderLinesModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that Add draws the lines again with an empty one after them, which takes the keyboard, and
    /// with none of the messages a send would give: HTMX gets the lines alone, the browser on its own the page.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="linesAlone">Whether the answer should be the lines alone.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Partial), true, DisplayName = "With HTMX")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "Without HTMX")]
    public void OnPostAddLine_EachKindOfRequest_DrawsAnotherFocusedLine(string kind, bool linesAlone)
    {
        // Arrange (Given)
        OrderLinesModel page = CreatePage(Enum.Parse<HtmxRequest>(kind));
        page.Input.Lines = [Line("Sample paper", 0)];
        page.ModelState.AddModelError("Input.Lines[0].Quantity", "Enter a quantity from 1 to 99.");

        // Act (When)
        IActionResult result = page.OnPostAddLine();

        // Assert (Then)
        Assert.AreEqual(
            (linesAlone ? "_OrderLines" : null, !linesAlone, true),
            ((result as PartialViewResult)?.ViewName, result is PageResult, page.ModelState.IsValid),
            "The answer should be the lines or the page, with no message from the post.");
        Assert.AreEqual(
            (2, "Sample paper", null, 1),
            (page.Input.Lines.Length, page.Input.Lines[0].Product, page.Input.Lines[1].Product, page.FocusedLine),
            "The line should be kept, and the new one, last, take the keyboard.");
    }

    /// <summary>
    /// Unit test to verify that Add makes no line once the order has the most it may.
    /// </summary>
    [TestMethod]
    public void OnPostAddLine_MostLines_AddsNone()
    {
        // Arrange (Given)
        OrderLinesModel page = CreatePage(HtmxRequest.Partial);
        page.Input.Lines = Enumerable.Range(1, OrderLinesModel.MostLines)
            .Select(static quantity => Line("Mock mugs", quantity))
            .ToArray();

        // Act (When)
        page.OnPostAddLine();

        // Assert (Then)
        Assert.HasCount(
            OrderLinesModel.MostLines,
            page.Input.Lines,
            "The order should keep the most lines it may, and no more.");
    }

    /// <summary>
    /// Unit test to verify that Remove takes its line away and gives the keyboard to the line now in its place, or to
    /// the new last line when the removed line was last.
    /// </summary>
    /// <param name="position">The position of the line removed.</param>
    /// <param name="left">The quantities of the lines left, joined by a space.</param>
    /// <param name="focused">The position of the line that should take the keyboard.</param>
    [TestMethod]
    [DataRow(0, "3 5", 0, DisplayName = "The first line")]
    [DataRow(1, "2 5", 1, DisplayName = "A middle line")]
    [DataRow(2, "2 3", 1, DisplayName = "The last line")]
    public void OnPostRemoveLine_ByHtmx_TakesTheLineAway(int position, string left, int focused)
    {
        // Arrange (Given)
        OrderLinesModel page = CreatePage(HtmxRequest.Partial);
        page.Input.Lines = [Line("Sample paper", 2), Line("Mock mugs", 3), Line("Placeholder pens", 5)];

        // Act (When)
        IActionResult result = page.OnPostRemoveLine(position);

        // Assert (Then)
        Assert.AreEqual(
            ("_OrderLines", left, focused),
            ((result as PartialViewResult)?.ViewName, Quantities(page.Input.Lines), page.FocusedLine),
            "The line should go, and the line in its place, or the last, take the keyboard.");
    }

    /// <summary>
    /// Unit test to verify that Remove forgets the values the form posted, so that each line left is drawn with its
    /// own values rather than with what the line once at its position posted, which a tag helper would write first.
    /// </summary>
    [TestMethod]
    public void OnPostRemoveLine_ValuesPosted_ForgetsThem()
    {
        // Arrange (Given)
        OrderLinesModel page = CreatePage(HtmxRequest.Partial);
        page.Input.Lines = [Line("Sample paper", 2), Line("Mock mugs", 3), Line("Placeholder pens", 5)];
        page.ModelState.SetModelValue("Input.Lines[1].Product", "Mock mugs", "Mock mugs");
        page.ModelState.SetModelValue("Input.Lines[1].Quantity", "3", "3");

        // Act (When)
        page.OnPostRemoveLine(1);

        // Assert (Then)
        Assert.AreEqual(
            (0, "Placeholder pens"),
            (page.ModelState.Count, page.Input.Lines[1].Product),
            "The posted values should be forgotten, and the line at position 1 be the third.");
    }

    /// <summary>
    /// Unit test to verify that Remove keeps the only line, or a line it is asked for by a position the order does not
    /// have, since a form always holds at least one line.
    /// </summary>
    /// <param name="lines">How many lines the order has.</param>
    /// <param name="position">The position Remove is asked for.</param>
    [TestMethod]
    [DataRow(1, 0, DisplayName = "The only line")]
    [DataRow(2, 7, DisplayName = "A position past the last")]
    [DataRow(2, -1, DisplayName = "A position before the first")]
    public void OnPostRemoveLine_NoLineToTake_KeepsEveryLine(int lines, int position)
    {
        // Arrange (Given)
        OrderLinesModel page = CreatePage(HtmxRequest.Partial);
        page.Input.Lines = Enumerable.Range(1, lines).Select(static quantity => Line("Mock mugs", quantity)).ToArray();

        // Act (When)
        page.OnPostRemoveLine(position);

        // Assert (Then)
        Assert.HasCount(
            lines,
            page.Input.Lines,
            "Every line should be kept.");
    }

    /// <summary>
    /// Unit test to verify that an order HTMX sent is received: the answer is a new form of one line, which takes the
    /// keyboard, and a toast that counts the lines and the items.
    /// </summary>
    [TestMethod]
    public void OnPost_OrderReceivedByHtmx_AnswersANewFormAndAToast()
    {
        // Arrange (Given)
        OrderLinesModel page = CreatePage(HtmxRequest.Partial);
        page.Input.Lines = [Line("Sample paper", 2), Line("Mock mugs", 3)];

        // Act (When)
        IActionResult result = page.OnPost();

        // Assert (Then)
        Assert.AreEqual(
            ("_OrderReceived", "Received an order of 2 lines and 5 items.", 1, 0),
            ((result as PartialViewResult)?.ViewName, page.Toast?.Message, page.Input.Lines.Length, page.FocusedLine),
            "HTMX should get a new form, its line taking the keyboard, and the toast.");
    }

    /// <summary>
    /// Unit test to verify that an order sent without HTMX is received and redirected back to the page, the toast left
    /// in TempData for it, so that refreshing the page does not send the order again.
    /// </summary>
    [TestMethod]
    public void OnPost_OrderReceivedWithoutHtmx_RedirectsWithTheToast()
    {
        // Arrange (Given)
        OrderLinesModel page = CreatePage(HtmxRequest.None);
        page.Input.Lines = [Line("Dummy desk lamps", 1)];

        // Act (When)
        IActionResult result = page.OnPost();

        // Assert (Then)
        Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "The browser should be sent back to the page.");
        Assert.AreEqual(
            "Received an order of 1 line and 1 item.",
            page.TempData["ToastMessage"],
            "The toast should wait in TempData for the page.");
    }

    /// <summary>
    /// Unit test to verify that a line naming a product the form does not offer, as a post the select did not send
    /// can, is refused with 422 and marked at its own line's product.
    /// </summary>
    [TestMethod]
    public void OnPost_ProductNotOffered_MarksItsLine()
    {
        // Arrange (Given)
        OrderLinesModel page = CreatePage(HtmxRequest.Partial);
        page.Input.Lines = [Line("Sample paper", 2), Line("Imaginary invoices", 1)];

        // Act (When)
        IActionResult result = page.OnPost();
        ModelStateEntry? entry = page.ModelState["Input.Lines[1].Product"];

        // Assert (Then)
        Assert.AreEqual(
            (StatusCodes.Status422UnprocessableEntity, "_OrderForm", "Choose a product from the list."),
            (page.Response.StatusCode, (result as PartialViewResult)?.ViewName, entry?.Errors[0].ErrorMessage),
            "The form should be refused, with the second line's product marked.");
    }

    /// <summary>
    /// Unit test to verify that the page's heading takes the keyboard only when HTMX loaded the whole page, as a
    /// boosted link does.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="focused">Whether the heading should take the keyboard.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Boosted), true, DisplayName = "A boosted link")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The browser on its own")]
    public void OnGet_EachKindOfRequest_FocusesTheHeadingForHtmx(string kind, bool focused)
    {
        // Arrange (Given)
        OrderLinesModel page = CreatePage(Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        page.OnGet();

        // Assert (Then)
        Assert.AreEqual(
            (focused, 1, null),
            (page.HeadingFocused, page.Input.Lines.Length, page.FocusedLine),
            "The page should show a form of one line, the heading focused only for HTMX.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Makes one of the order's lines.
    /// </summary>
    /// <param name="product">The line's product.</param>
    /// <param name="quantity">The line's quantity.</param>
    /// <returns>The line.</returns>
    private static OrderLineInput Line(string product, int quantity)
    {
        return new OrderLineInput { Product = product, Quantity = quantity };
    }

    /// <summary>
    /// Writes the lines' quantities in order, joined by a space, as a test compares them.
    /// </summary>
    /// <param name="lines">The lines.</param>
    /// <returns>The quantities, such as <c>2 5</c>.</returns>
    private static string Quantities(OrderLineInput[] lines)
    {
        return string.Join(' ', lines.Select(static line => line.Quantity));
    }

    /// <summary>
    /// Creates the page as the framework would for one request: the headers HTMX 4 sends for that kind of request,
    /// TempData, and what a partial needs to render.
    /// </summary>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static OrderLinesModel CreatePage(HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Components/OrderLines");
        return new OrderLinesModel
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
        };
    }

    #endregion Private Methods
}
