namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="ConfirmDeleteModel"/> class: a file's confirmation
/// page, which asks its dialog's question as a page of its own.
/// </summary>
[TestClass]
public sealed class ConfirmDeleteModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the page asks about the file its address names, its heading taking the keyboard only
    /// when HTMX loaded it.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="focused">Whether the heading should take the keyboard.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Boosted), true, DisplayName = "A boosted link")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The browser on its own")]
    public void OnGet_AFileThere_AsksAboutIt(string kind, bool focused)
    {
        // Arrange (Given)
        ConfirmDeleteModel page = CreatePage(new SavedFiles(), Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        page.OnGet(10003);

        // Assert (Then)
        Assert.AreEqual(
            ("Mock photo.png", StatusCodes.Status200OK, focused),
            (page.Asked?.Name, page.Response.StatusCode, page.HeadingFocused),
            "The page should ask about the file, the heading focused only for HTMX.");
    }

    /// <summary>
    /// Unit test to verify that a file already gone gets the page saying so, with 404.
    /// </summary>
    [TestMethod]
    public void OnGet_AFileGone_SaysSoWithNotFound()
    {
        // Arrange (Given)
        SavedFiles files = new SavedFiles();
        files.Delete(10003);
        ConfirmDeleteModel page = CreatePage(files, HtmxRequest.None);

        // Act (When)
        page.OnGet(10003);

        // Assert (Then)
        Assert.AreEqual(
            (null, StatusCodes.Status404NotFound),
            (page.Asked, page.Response.StatusCode),
            "A file already gone should answer 404, asking nothing.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the page as the framework would for one request, with the headers HTMX 4 sends for that kind of
    /// request.
    /// </summary>
    /// <param name="files">The files.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static ConfirmDeleteModel CreatePage(SavedFiles files, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Components/ConfirmDelete");
        return new ConfirmDeleteModel(files)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
        };
    }

    #endregion Private Methods
}
