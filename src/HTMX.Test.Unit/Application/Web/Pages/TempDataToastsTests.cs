namespace HTMX.Test.Unit.Application.Web.Pages;

using HTMX.Application.Web.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="TempDataToasts"/> class: a toast left in TempData
/// comes back as it was left, and nothing comes back when none was.
/// </summary>
[TestClass]
public sealed class TempDataToastsTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a toast with Undo comes back with its message, its Undo and the element Undo replaces.
    /// </summary>
    [TestMethod]
    public void Take_ToastLeftWithUndo_ReturnsItAsItWasLeft()
    {
        // Arrange (Given)
        ITempDataDictionary tempData = CreateTempData();
        Toast left = new Toast
        {
            Message = "Pat Example archived.",
            UndoAction = "/users/10007?handler=Restore",
            UndoTarget = "#user",
        };
        TempDataToasts.Leave(tempData, left);

        // Act (When)
        Toast? taken = TempDataToasts.Take(tempData, "#elsewhere");

        // Assert (Then)
        Assert.AreEqual(
            left,
            taken,
            "The toast should come back as it was left.");
    }

    /// <summary>
    /// Unit test to verify that a toast with no Undo names no element for it, and that TempData with no toast gives
    /// none.
    /// </summary>
    [TestMethod]
    public void Take_ToastWithoutUndoOrNone_ReturnsNoUndoOrNothing()
    {
        // Arrange (Given)
        ITempDataDictionary left = CreateTempData();
        TempDataToasts.Leave(left, new Toast { Message = "Casey Mockup deleted." });
        ITempDataDictionary empty = CreateTempData();

        // Act (When)
        Toast? deleted = TempDataToasts.Take(left, "#user-list");
        Toast? none = TempDataToasts.Take(empty, "#user-list");

        // Assert (Then)
        Assert.AreEqual(
            new Toast { Message = "Casey Mockup deleted." },
            deleted,
            "A toast with nothing to undo should name no element for Undo.");
        Assert.IsNull(
            none,
            "TempData with no toast should give none.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the TempData of one request, with nothing waiting in it.
    /// </summary>
    /// <returns>The TempData.</returns>
    private static TempDataDictionary CreateTempData()
    {
        return new TempDataDictionary(new DefaultHttpContext(), new StubTempDataProvider());
    }

    #endregion Private Methods
}
