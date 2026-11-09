namespace HTMX.Test.Unit.Application.Web.Validation;

using HTMX.Application.Web.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="AbsoluteWebAddressAttribute"/> class: the form's
/// check that a website can become a <c>WebAddress</c>.
/// </summary>
[TestClass]
public sealed class AbsoluteWebAddressAttributeTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the form accepts what a web address holds, spaces around it, and no value at all.
    /// </summary>
    /// <param name="value">The value as bound.</param>
    [TestMethod]
    [DataRow("https://placeholder.example", DisplayName = "https")]
    [DataRow("  http://placeholder.example  ", DisplayName = "http with spaces around it")]
    [DataRow(null, DisplayName = "Left empty")]
    public void IsValid_WebAddressOrNothing_Accepts(string? value)
    {
        // Arrange (Given)
        AbsoluteWebAddressAttribute rule = new AbsoluteWebAddressAttribute();

        // Act (When)
        bool valid = rule.IsValid(value);

        // Assert (Then)
        Assert.IsTrue(
            valid,
            "An absolute web address, or no value, should be accepted.");
    }

    /// <summary>
    /// Unit test to verify that the form refuses whatever a web address would refuse, so a save never fails on it.
    /// </summary>
    /// <param name="value">The value as bound.</param>
    [TestMethod]
    [DataRow("placeholder.example", DisplayName = "No scheme")]
    [DataRow("placeholder dot example", DisplayName = "Words")]
    [DataRow("ftp://placeholder.example", DisplayName = "Not a web scheme")]
    [DataRow("mailto:pat@example.com", DisplayName = "No host")]
    public void IsValid_NotAWebAddress_Refuses(string value)
    {
        // Arrange (Given)
        AbsoluteWebAddressAttribute rule = new AbsoluteWebAddressAttribute();

        // Act (When)
        bool valid = rule.IsValid(value);

        // Assert (Then)
        Assert.IsFalse(
            valid,
            "Text that is not an absolute web address should be refused.");
    }

    #endregion Public Methods
}
