namespace HTMX.Test.Unit.Application.Web.Pages.Components.Card;

using System;
using System.Linq;
using HTMX.Application.Web.Pages.Components.Card;
using HTMX.Test.Unit.Application.Web.TagHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="CardTagHelper"/> class: the utilities the card and
/// each of its parts get, that a class the view wrote is kept and the attribute taken away, and that a part the card
/// does not have is refused.
/// </summary>
[TestClass]
public sealed class CardTagHelperTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the card gives the part its attribute names that part's own utilities, and gives the
    /// card itself its utilities when the attribute has no value.
    /// </summary>
    /// <param name="part">
    /// The part the attribute names, or <see langword="null"/> for an attribute with no value.
    /// </param>
    /// <param name="utility">A utility that belongs to that part alone.</param>
    [TestMethod]
    [DataRow(null, "shadow-card", DisplayName = "Card")]
    [DataRow(null, "@container", DisplayName = "Card, a size container")]
    [DataRow(null, "[&+&]:mt-4", DisplayName = "Card, 16px below a card above it")]
    [DataRow("header", "border-b", DisplayName = "Header")]
    [DataRow("header", "[&>h2]:text-lg", DisplayName = "Header, its title")]
    [DataRow("body", "p-4", DisplayName = "Body")]
    public void Process_PartNamed_AddsThePartsUtilities(string? part, string utility)
    {
        // Arrange (Given)
        TagHelperAttribute attribute = part is null
            ? new TagHelperAttribute("card")
            : new TagHelperAttribute("card", new HtmlString(part));

        // Act (When)
        TagHelperOutput output = ComponentRuns.Process(new CardTagHelper(), attribute);
        bool added = ComponentRuns.ClassesOf(output).Contains(utility);

        // Assert (Then)
        Assert.IsTrue(
            added,
            $"The part should have the utility {utility}.");
    }

    /// <summary>
    /// Unit test to verify that a card with no value draws the card itself, keeps the class the view gave it, and
    /// leaves its attribute out of the page.
    /// </summary>
    [TestMethod]
    public void Process_NoValue_DrawsTheCardAndKeepsTheViewsClass()
    {
        // Arrange (Given)
        TagHelperAttribute attribute = new TagHelperAttribute("card");
        TagHelperAttribute viewClass = new TagHelperAttribute("class", "mt-4");

        // Act (When)
        TagHelperOutput output = ComponentRuns.Process(new CardTagHelper(), attribute, viewClass);
        string[] classes = ComponentRuns.ClassesOf(output);
        bool card = classes.Contains("shadow-card");
        bool kept = classes.Contains("mt-4");
        bool header = classes.Contains("border-b");
        bool attributeLeft = output.Attributes.ContainsName("card");

        // Assert (Then)
        Assert.AreEqual(
            (true, true, false, false),
            (card, kept, header, attributeLeft),
            "The card should have its own utilities and the view's class, and lose its attribute.");
    }

    /// <summary>
    /// Unit test to verify that a part the card does not have is refused rather than drawn as some other part.
    /// </summary>
    [TestMethod]
    public void Process_UnknownPart_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        TagHelperAttribute attribute = new TagHelperAttribute("card", new HtmlString("sidebar"));
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            ComponentRuns.Process(new CardTagHelper(), attribute);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A part the card does not have should be refused.");
    }

    #endregion Public Methods
}
