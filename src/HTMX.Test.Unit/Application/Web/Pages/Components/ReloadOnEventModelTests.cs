namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="ReloadOnEventModel"/> class: a form whose answer
/// raises an event, and a list that asks for itself again when it hears it.
/// </summary>
[TestClass]
public sealed class ReloadOnEventModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a note written with HTMX is on the board, and the answer is the form, empty again, its
    /// field taking the keyboard, with HX-Trigger naming the event the list listens for.
    /// </summary>
    [TestMethod]
    public void OnPost_WrittenByHtmx_AnswersTheFormAndRaisesTheEvent()
    {
        // Arrange (Given)
        NoteBoard board = new NoteBoard();
        ReloadOnEventModel page = CreatePage(board, HtmxRequest.Partial);
        page.Input = new NoteInput { Text = "Call the depot about Thursday." };

        // Act (When)
        IActionResult result = page.OnPost();
        IHeaderDictionary headers = page.Response.Headers;

        // Assert (Then)
        string trigger = headers["HX-Trigger"].ToString();
        Assert.AreEqual(
            ("_NoteForm", "notes-changed", string.Empty, true, 1),
            ((result as PartialViewResult)?.ViewName, trigger, page.Input.Text, page.TextFocused, board.Newest().Length),
            "The note should be written, and the empty form answered with the event.");
    }

    /// <summary>
    /// Unit test to verify that a refused note answers 422 with the form and raises nothing, since nothing changed.
    /// </summary>
    [TestMethod]
    public void OnPost_Refused_RaisesNothing()
    {
        // Arrange (Given)
        NoteBoard board = new NoteBoard();
        ReloadOnEventModel page = CreatePage(board, HtmxRequest.Partial);
        page.ModelState.AddModelError("Input.Text", "Write a note.");

        // Act (When)
        page.OnPost();
        IHeaderDictionary headers = page.Response.Headers;

        // Assert (Then)
        Assert.AreEqual(
            (StatusCodes.Status422UnprocessableEntity, false, 0),
            (page.Response.StatusCode, headers.ContainsKey("HX-Trigger"), board.Newest().Length),
            "A refused note should get 422, write nothing and raise no event.");
    }

    /// <summary>
    /// Unit test to verify that HTMX asking for part of the page, as the list does when it hears the event, gets the
    /// list alone, with the notes newest first, and the browser on its own gets the whole page.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="listAlone">Whether the answer should be the list alone.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Partial), true, DisplayName = "The list asking for itself")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The browser on its own")]
    public void OnGet_EachKindOfRequest_AnswersTheListOrThePage(string kind, bool listAlone)
    {
        // Arrange (Given)
        NoteBoard board = new NoteBoard();
        board.Write("A note.");
        ReloadOnEventModel page = CreatePage(board, Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        IActionResult result = page.OnGet();

        // Assert (Then)
        Assert.AreEqual(
            (listAlone ? "_NoteList" : null, !listAlone, 1),
            ((result as PartialViewResult)?.ViewName, result is PageResult, page.Notes.Length),
            "The list should get itself alone, and the browser the page, with the notes.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the page as the framework would for one request: the headers HTMX 4 sends for that kind of request, and
    /// what a partial needs to render.
    /// </summary>
    /// <param name="board">The notes.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static ReloadOnEventModel CreatePage(NoteBoard board, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Components/ReloadOnEvent");
        return new ReloadOnEventModel(board)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
        };
    }

    #endregion Private Methods
}
