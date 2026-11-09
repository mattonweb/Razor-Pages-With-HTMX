namespace HTMX.Test.Unit.Adapter.Customers.Contracts;

using System;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="WebAddress"/> struct: which addresses it holds
/// and which it refuses when it is made.
/// </summary>
[TestClass]
public sealed class WebAddressTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that an absolute http or https address with a host is held as it was given.
    /// </summary>
    /// <param name="value">The address.</param>
    [TestMethod]
    [DataRow("https://placeholder.example", DisplayName = "https")]
    [DataRow("http://placeholder.example/about?page=1", DisplayName = "http with a path and query")]
    public void Constructor_AbsoluteWebAddress_HoldsIt(string value)
    {
        // Arrange (Given)
        // The address comes from the data row.

        // Act (When)
        WebAddress address = new WebAddress(value);

        // Assert (Then)
        Assert.AreEqual(
            value,
            address.Value,
            "The address should be held as it was given.");
    }

    /// <summary>
    /// Unit test to verify that anything other than an absolute http or https address with a host is refused where it
    /// enters, so a page can always link to it and read its host.
    /// </summary>
    /// <param name="value">The text that is not a web address.</param>
    [TestMethod]
    [DataRow("placeholder.example", DisplayName = "No scheme")]
    [DataRow("placeholder dot example", DisplayName = "Words")]
    [DataRow("ftp://placeholder.example", DisplayName = "Not a web scheme")]
    [DataRow("mailto:pat@example.com", DisplayName = "No host")]
    [DataRow("/organizations", DisplayName = "Relative")]
    public void Constructor_NotAnAbsoluteWebAddress_Throws(string value)
    {
        // Arrange (Given)
        // The text comes from the data row.

        // Act (When)
        ArgumentException thrown = Assert.ThrowsExactly<ArgumentException>(
            () => new WebAddress(value),
            "Text that is not an absolute web address should be refused.");

        // Assert (Then)
        Assert.AreEqual(
            "value",
            thrown.ParamName,
            "The exception should name the parameter.");
    }

    #endregion Public Methods
}
