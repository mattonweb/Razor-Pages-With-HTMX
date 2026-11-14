namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using HTMX.Application.Web.Pages.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="NoticeBoard"/> class: the Seen notices page's
/// notices, each new until it has been seen.
/// </summary>
[TestClass]
public sealed class NoticeBoardTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that marking a notice seen marks that notice alone.
    /// </summary>
    [TestMethod]
    public void MarkSeen_KnownNotice_MarksItAlone()
    {
        // Arrange (Given)
        NoticeBoard board = new NoticeBoard();

        // Act (When)
        bool marked = board.MarkSeen(10005);
        Notice[] notices = board.All();

        // Assert (Then)
        Assert.AreEqual(
            (true, true, 1),
            (marked, notices[4].Seen, Array.FindAll(notices, static notice => notice.Seen).Length),
            "The notice should be seen, and no other.");
    }

    /// <summary>
    /// Unit test to verify that marking a notice the board does not have marks nothing and says so.
    /// </summary>
    [TestMethod]
    public void MarkSeen_UnknownNotice_ReturnsFalse()
    {
        // Arrange (Given)
        NoticeBoard board = new NoticeBoard();

        // Act (When)
        bool marked = board.MarkSeen(99);

        // Assert (Then)
        Assert.AreEqual(
            (false, 0),
            (marked, Array.FindAll(board.All(), static notice => notice.Seen).Length),
            "Nothing should be marked.");
    }

    /// <summary>
    /// Unit test to verify that every notice can be marked seen, and every notice new again.
    /// </summary>
    /// <param name="seen">Whether the notices are marked seen.</param>
    [TestMethod]
    [DataRow(true, DisplayName = "All seen")]
    [DataRow(false, DisplayName = "All new")]
    public void MarkAll_EitherWay_MarksEveryNotice(bool seen)
    {
        // Arrange (Given)
        NoticeBoard board = new NoticeBoard();
        board.MarkSeen(10001);

        // Act (When)
        board.MarkAll(seen);

        // Assert (Then)
        Assert.IsTrue(
            Array.TrueForAll(board.All(), notice => notice.Seen == seen),
            "Every notice should be marked the same.");
    }

    #endregion Public Methods
}
