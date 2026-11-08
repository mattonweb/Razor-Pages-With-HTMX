namespace HTMX.Test.Unit.Application.Web.Entities;

using System;
using HTMX.Application.Web.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="DatabaseName"/> struct.
/// </summary>
[TestClass]
public sealed class DatabaseNameTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a name cannot be built from nothing, so no database is ever opened without a name.
    /// </summary>
    [TestMethod]
    public void Constructor_NullValue_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        string? value = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new DatabaseName(value!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A null name should be refused with ArgumentNullException.");
    }

    /// <summary>
    /// Unit test to verify that a name made only of spaces is refused, because SQLite would treat it as a real name
    /// that nobody could read on screen.
    /// </summary>
    [TestMethod]
    public void Constructor_WhiteSpaceValue_ThrowsArgumentException()
    {
        // Arrange (Given)
        string value = "   ";
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new DatabaseName(value);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A name of white space should be refused with ArgumentException.");
    }

    /// <summary>
    /// Unit test to verify that a valid name is kept exactly as given, since connections match databases by it.
    /// </summary>
    [TestMethod]
    public void Value_ValidName_ReturnsTheName()
    {
        // Arrange (Given)
        string value = "HtmxPages";

        // Act (When)
        DatabaseName name = new DatabaseName(value);

        // Assert (Then)
        Assert.AreEqual(
            value,
            name.Value,
            "The name should be returned exactly as it was given.");
    }

    #endregion Public Methods
}
