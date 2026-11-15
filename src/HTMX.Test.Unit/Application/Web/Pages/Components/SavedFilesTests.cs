namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using HTMX.Application.Web.Pages.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="SavedFiles"/> class: the Confirmation page's files,
/// which a delete removes for good until Put them all back.
/// </summary>
[TestClass]
public sealed class SavedFilesTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a delete removes its file alone, and says which it removed.
    /// </summary>
    [TestMethod]
    public void Delete_AFile_RemovesItAlone()
    {
        // Arrange (Given)
        SavedFiles files = new SavedFiles();

        // Act (When)
        SavedFile? deleted = files.Delete(10002);
        long[] left = Array.ConvertAll(files.All(), static file => file.Number);

        // Assert (Then)
        Assert.AreEqual(
            ("Sample contract.pdf", null),
            (deleted?.Name, files.Find(10002)),
            "The file should be removed.");
        CollectionAssert.AreEqual(
            new long[] { 10001, 10003 },
            left,
            "The other files should stay, in order.");
    }

    /// <summary>
    /// Unit test to verify that deleting a file the list does not have removes nothing and says so.
    /// </summary>
    [TestMethod]
    public void Delete_UnknownFile_ReturnsNull()
    {
        // Arrange (Given)
        SavedFiles files = new SavedFiles();

        // Act (When)
        SavedFile? deleted = files.Delete(99);

        // Assert (Then)
        Assert.AreEqual(
            (null, 3),
            (deleted, files.All().Length),
            "Nothing should be removed.");
    }

    /// <summary>
    /// Unit test to verify that Put them all back brings every file back, in its first order.
    /// </summary>
    [TestMethod]
    public void PutBack_AfterDeletes_BringsEveryFileBack()
    {
        // Arrange (Given)
        SavedFiles files = new SavedFiles();
        files.Delete(10001);
        files.Delete(10003);

        // Act (When)
        files.PutBack();
        long[] back = Array.ConvertAll(files.All(), static file => file.Number);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new long[] { 10001, 10002, 10003 },
            back,
            "Every file should be back, in order.");
    }

    #endregion Public Methods
}
