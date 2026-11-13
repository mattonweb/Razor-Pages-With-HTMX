namespace HTMX.Test.Unit.Application.Web.Validation;

using System.ComponentModel.DataAnnotations;
using HTMX.Application.Web.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="TelephoneNumbersAttribute"/> class: the form's
/// check of the telephone numbers its rows post.
/// </summary>
[TestClass]
public sealed class TelephoneNumbersAttributeTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that numbers, spaces around them and rows left empty are accepted, up to five numbers.
    /// </summary>
    [TestMethod]
    public void GetValidationResult_FiveNumbersAndEmptyRows_Accepts()
    {
        // Arrange (Given)
        string?[] rows =
        [
            "+1 555-0101", " +1 555-0102 ", null, "+1 555-0103", string.Empty, "+1 555-0104", "+1 555-0105",
        ];

        // Act (When)
        ValidationResult? result = Check(rows);

        // Assert (Then)
        Assert.AreEqual(
            ValidationResult.Success,
            result,
            "Five numbers, with empty rows among them, should be accepted.");
    }

    /// <summary>
    /// Unit test to verify that more than five numbers, or a value that is not a telephone number, are refused with a
    /// message that says which.
    /// </summary>
    /// <param name="rows">The rows' values, separated by commas.</param>
    /// <param name="message">The start of the message expected.</param>
    [TestMethod]
    [DataRow(
        "+1 555-0101,+1 555-0102,+1 555-0103,+1 555-0104,+1 555-0105,+1 555-0106",
        "Keep it to five",
        DisplayName = "Six numbers")]
    [DataRow("+1 555-0101,call me", "Enter each telephone number", DisplayName = "Words")]
    public void GetValidationResult_TooManyOrNotANumber_Refuses(string rows, string message)
    {
        // Arrange (Given)
        string?[] values = rows.Split(',');

        // Act (When)
        ValidationResult? result = Check(values);

        // Assert (Then)
        StringAssert.StartsWith(
            result?.ErrorMessage,
            message,
            "The rows should be refused with the message that says why.");
    }

    /// <summary>
    /// Unit test to verify the rule one row is marked by: a telephone number of 40 characters or fewer, or nothing.
    /// </summary>
    /// <param name="value">The row's value.</param>
    /// <param name="expected">Whether the row should pass.</param>
    [TestMethod]
    [DataRow("+1 555-0142", true, DisplayName = "A number")]
    [DataRow(null, true, DisplayName = "Left empty")]
    [DataRow("call me", false, DisplayName = "Words")]
    [DataRow("+1 555 0142 0142 0142 0142 0142 0142 0142 0142", false, DisplayName = "Longer than 40")]
    public void IsWellFormed_EachKindOfValue_SaysWhetherItPasses(string? value, bool expected)
    {
        // Arrange (Given)
        string? row = value;

        // Act (When)
        bool wellFormed = TelephoneNumbersAttribute.IsWellFormed(row);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            wellFormed,
            "A row should pass when it holds a telephone number of 40 characters or fewer, or nothing.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Checks rows as the framework would check the property the attribute marks.
    /// </summary>
    /// <param name="rows">The rows' values.</param>
    /// <returns>The result.</returns>
    private static ValidationResult? Check(string?[] rows)
    {
        TelephoneNumbersAttribute rule = new TelephoneNumbersAttribute();
        ValidationContext context = new ValidationContext(rows) { MemberName = "PhoneNumbers" };
        return rule.GetValidationResult(rows, context);
    }

    #endregion Private Methods
}
