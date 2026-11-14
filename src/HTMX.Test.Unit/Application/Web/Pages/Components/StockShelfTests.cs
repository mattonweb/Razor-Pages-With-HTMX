namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using HTMX.Application.Web.Pages.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="StockShelf"/> class: the Stock table page's items
/// and how many of each are on hand.
/// </summary>
[TestClass]
public sealed class StockShelfTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a count is recorded for its item alone, and read back.
    /// </summary>
    [TestMethod]
    public void Count_KnownItem_RecordsItsCount()
    {
        // Arrange (Given)
        StockShelf shelf = new StockShelf();

        // Act (When)
        StockItem? counted = shelf.Count(10002, 0);

        // Assert (Then)
        Assert.AreEqual(
            (0, 0, 120),
            (counted?.OnHand, shelf.Find(10002)?.OnHand, shelf.Find(10001)?.OnHand),
            "The item should hold its new count, and the others theirs.");
    }

    /// <summary>
    /// Unit test to verify that a count for an item the shelf does not have records nothing and says so.
    /// </summary>
    [TestMethod]
    public void Count_UnknownItem_ReturnsNull()
    {
        // Arrange (Given)
        StockShelf shelf = new StockShelf();

        // Act (When)
        StockItem? counted = shelf.Count(99, 5);

        // Assert (Then)
        Assert.IsNull(
            counted,
            "The shelf should have no item to count.");
    }

    /// <summary>
    /// Unit test to verify that a count below zero is refused, since a handler checks the count before it saves.
    /// </summary>
    [TestMethod]
    public void Count_BelowZero_Throws()
    {
        // Arrange (Given)
        StockShelf shelf = new StockShelf();

        // Act (When)
        ArgumentOutOfRangeException thrown = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => shelf.Count(10001, -1),
            "A count below zero should be refused.");

        // Assert (Then)
        Assert.AreEqual(
            (120, "onHand"),
            (shelf.Find(10001)?.OnHand, thrown.ParamName),
            "The item should keep its count, and the exception name the count.");
    }

    #endregion Public Methods
}
