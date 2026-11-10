namespace HTMX.Test.Unit.Application.Web.Pages.Users;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HTMX.Application.Web.Pages.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the rules of the <see cref="UserInput"/> class that a value type does not hold
/// itself: which usernames the form accepts.
/// </summary>
[TestClass]
public sealed class UserInputTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a username of letters, digits, dots, hyphens and underscores is accepted, with space
    /// around it, which the form trims.
    /// </summary>
    /// <param name="username">The username as bound.</param>
    [TestMethod]
    [DataRow("pat.example", DisplayName = "A first and last name")]
    [DataRow("alexandria.wireframe-boilerplate", DisplayName = "With a hyphen")]
    [DataRow("kit_sandbox2", DisplayName = "With an underscore and a digit")]
    [DataRow("  pat.example  ", DisplayName = "With spaces around it")]
    public void Username_LettersDigitsAndMarks_IsAccepted(string username)
    {
        // Arrange (Given)
        UserInput input = new UserInput { Name = "Pat Example", Username = username, Email = "pat@example.com" };

        // Act (When)
        List<ValidationResult> results = Validate(input);

        // Assert (Then)
        Assert.IsEmpty(
            results,
            "A username of letters, digits, dots, hyphens and underscores should be accepted.");
    }

    /// <summary>
    /// Unit test to verify that a username with a space or another mark inside it, an accented letter, none at all, or
    /// one too long is refused.
    /// </summary>
    /// <param name="username">The username as bound.</param>
    [TestMethod]
    [DataRow("pat example", DisplayName = "A space inside it")]
    [DataRow("pat@example", DisplayName = "An at sign")]
    [DataRow("renée.exemple", DisplayName = "An accented letter")]
    [DataRow("", DisplayName = "Left empty")]
    [DataRow("a.username.that.runs.on.well.past.the.fifty.characters", DisplayName = "Longer than 50")]
    public void Username_OtherText_IsRefused(string username)
    {
        // Arrange (Given)
        UserInput input = new UserInput { Name = "Pat Example", Username = username, Email = "pat@example.com" };

        // Act (When)
        List<ValidationResult> results = Validate(input);

        // Assert (Then)
        Assert.HasCount(
            1,
            results,
            "Only the username should be refused.");
        CollectionAssert.AreEqual(
            new[] { nameof(UserInput.Username) },
            new List<string>(results[0].MemberNames),
            "The message should belong to the Username field.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Validates every value of the form, as model binding does.
    /// </summary>
    /// <param name="input">The form's values.</param>
    /// <returns>What failed, if anything.</returns>
    private static List<ValidationResult> Validate(UserInput input)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);
        return results;
    }

    #endregion Private Methods
}
