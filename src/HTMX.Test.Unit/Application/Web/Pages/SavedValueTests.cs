namespace HTMX.Test.Unit.Application.Web.Pages;

using HTMX.Application.Web.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="SavedValue"/> class: what a field shows under its
/// control after another save overtook the one being made.
/// </summary>
[TestClass]
public sealed class SavedValueTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a saved value is shown only when it differs from the one being edited, compared as the
    /// form saves them, and that a value the other save emptied reads as the words for none.
    /// </summary>
    /// <param name="saved">The value the other save left.</param>
    /// <param name="edited">The value being edited.</param>
    /// <param name="expected">What the field shows, or nothing.</param>
    [TestMethod]
    [DataRow("Head of Samples", "Head of Examples", "Head of Samples", DisplayName = "A different value")]
    [DataRow("Head of Samples", " Head of Samples ", null, DisplayName = "The same value, with spaces")]
    [DataRow(null, "Head of Examples", "no job title", DisplayName = "Emptied by the other save")]
    [DataRow(null, " ", null, DisplayName = "Empty on both sides")]
    public void Hint_SavedAndEditedValues_ShowsOnlyADifference(string? saved, string? edited, string? expected)
    {
        // Arrange (Given)
        const string None = "no job title";

        // Act (When)
        string? hint = SavedValue.Hint(saved, edited, None);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            hint,
            "The field should show the saved value only when it differs from the one being edited.");
    }

    #endregion Public Methods
}
