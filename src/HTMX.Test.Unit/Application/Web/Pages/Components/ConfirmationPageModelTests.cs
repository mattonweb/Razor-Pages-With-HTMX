namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="ConfirmationPageModel"/> class: a list of files
/// whose Delete, asked first in a dialog or on a page, removes a file for good, with HTMX and without it.
/// </summary>
[TestClass]
public sealed class ConfirmationPageModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a delete HTMX sent from a file's dialog answers the list as it is now and a toast, the
    /// keyboard going to the Delete of the row now in the deleted row's place, or of the last row when it was last.
    /// </summary>
    /// <param name="file">The file deleted.</param>
    /// <param name="focused">The file whose Delete should take the keyboard.</param>
    [TestMethod]
    [DataRow(10002L, 10003L, DisplayName = "A row in the middle")]
    [DataRow(10003L, 10002L, DisplayName = "The last row")]
    public void OnPostDelete_ByHtmx_AnswersTheListWithTheKeyboardInPlace(long file, long focused)
    {
        // Arrange (Given)
        ConfirmationPageModel page = CreatePage(new SavedFiles(), HtmxRequest.Partial);

        // Act (When)
        IActionResult result = page.OnPostDelete(file);

        // Assert (Then)
        Assert.AreEqual(
            ("_SavedFilesAnswer", 2, focused, false),
            ((result as PartialViewResult)?.ViewName, page.Files.Length, page.FocusedFile, page.ListHeadingFocused),
            "HTMX should get the list without the file, the keyboard on the row in its place.");
        Assert.EndsWith(
            "deleted for good.",
            page.Toast?.Message ?? string.Empty,
            "The toast should say the file is gone for good.");
    }

    /// <summary>
    /// Unit test to verify that deleting the only file left puts the keyboard on the list's heading.
    /// </summary>
    [TestMethod]
    public void OnPostDelete_TheLastFileLeft_FocusesTheListsHeading()
    {
        // Arrange (Given)
        SavedFiles files = new SavedFiles();
        files.Delete(10001);
        files.Delete(10002);
        ConfirmationPageModel page = CreatePage(files, HtmxRequest.Partial);

        // Act (When)
        page.OnPostDelete(10003);

        // Assert (Then)
        Assert.AreEqual(
            (0, null, true),
            (page.Files.Length, page.FocusedFile, page.ListHeadingFocused),
            "With no row left, the list's heading should take the keyboard.");
    }

    /// <summary>
    /// Unit test to verify that a delete sent without HTMX, or from the confirmation page's form, is redirected back to
    /// the list, its toast left in TempData.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.None), DisplayName = "The browser on its own")]
    [DataRow(nameof(HtmxRequest.Boosted), DisplayName = "The confirmation page's boosted form")]
    public void OnPostDelete_NotForTheList_RedirectsWithTheToast(string kind)
    {
        // Arrange (Given)
        SavedFiles files = new SavedFiles();
        ConfirmationPageModel page = CreatePage(files, Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        IActionResult result = page.OnPostDelete(10001);

        // Assert (Then)
        Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "The browser should be sent back to the list.");
        Assert.AreEqual(
            ("Placeholder invoice.pdf deleted for good.", null),
            (page.TempData["ToastMessage"], files.Find(10001)),
            "The file should be gone, and the toast wait in TempData for the list.");
    }

    /// <summary>
    /// Unit test to verify that deleting a file already gone, as from a page left open in another tab, says so.
    /// </summary>
    [TestMethod]
    public void OnPostDelete_AlreadyGone_SaysSo()
    {
        // Arrange (Given)
        SavedFiles files = new SavedFiles();
        files.Delete(10002);
        ConfirmationPageModel page = CreatePage(files, HtmxRequest.Partial);

        // Act (When)
        page.OnPostDelete(10002);

        // Assert (Then)
        Assert.AreEqual(
            "That file was already deleted.",
            page.Toast?.Message,
            "The toast should say the file was already gone.");
    }

    /// <summary>
    /// Unit test to verify that Put them all back brings the files back and moves no keyboard, since its button keeps
    /// its id.
    /// </summary>
    [TestMethod]
    public void OnPostPutBack_ByHtmx_BringsTheFilesBack()
    {
        // Arrange (Given)
        SavedFiles files = new SavedFiles();
        files.Delete(10001);
        ConfirmationPageModel page = CreatePage(files, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = page.OnPostPutBack();
        string? view = (result as PartialViewResult)?.ViewName;

        // Assert (Then)
        Assert.AreEqual(
            ("_SavedFilesAnswer", 3, null, false, "Every file is back."),
            (view, page.Files.Length, page.FocusedFile, page.ListHeadingFocused, page.Toast?.Message),
            "HTMX should get every file back, the keyboard left where it is.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the page as the framework would for one request: the headers HTMX 4 sends for that kind of request,
    /// TempData, and what a partial needs to render.
    /// </summary>
    /// <param name="files">The files.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static ConfirmationPageModel CreatePage(SavedFiles files, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Components/ConfirmationPage");
        return new ConfirmationPageModel(files)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
        };
    }

    #endregion Private Methods
}
