namespace HTMX.Test.Unit.Application.Web.TagHelpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HTMX.Application.Web.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="ButtonTagHelper"/> class: the utilities each look
/// gives an element, that a class the view wrote is kept, and that a look it does not know is refused.
/// </summary>
[TestClass]
public sealed class ButtonTagHelperTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a primary button gets the utilities every button shares and the primary look's own,
    /// and keeps the class the view gave it.
    /// </summary>
    [TestMethod]
    public void Process_PrimaryLook_AddsTheSharedAndPrimaryUtilities()
    {
        // Arrange (Given)
        TagHelperOutput output = CreateOutput(new TagHelperAttribute("class", "ml-auto"));

        // Act (When)
        Process("primary", output);
        string[] classes = ClassesOf(output);
        bool shared = classes.Contains("inline-flex");
        bool primary = classes.Contains("bg-accent-fill");
        bool kept = classes.Contains("ml-auto");
        bool secondary = classes.Contains("bg-surface");

        // Assert (Then)
        Assert.AreEqual(
            (true, true, true, false),
            (shared, primary, kept, secondary),
            "A primary button should have the shared utilities, the primary fill and the view's class alone.");
    }

    /// <summary>
    /// Unit test to verify that an icon button is square, as wide as it is high, with no padding.
    /// </summary>
    [TestMethod]
    public void Process_IconLook_IsSquare()
    {
        // Arrange (Given)
        TagHelperOutput output = CreateOutput();

        // Act (When)
        Process("icon", output);
        string[] classes = ClassesOf(output);

        // Assert (Then)
        Assert.AreEqual(
            (true, true, false),
            (classes.Contains("w-control"), classes.Contains("px-0"), classes.Contains("px-3")),
            "An icon button should be as wide as a control is high, with no padding.");
    }

    /// <summary>
    /// Unit test to verify that the icon look for an inverse surface, such as a toast, is square and takes that
    /// surface's text color rather than the icon color.
    /// </summary>
    [TestMethod]
    public void Process_IconInverseLook_IsSquareInTheInverseText()
    {
        // Arrange (Given)
        TagHelperOutput output = CreateOutput();

        // Act (When)
        Process("icon-inverse", output);
        string[] classes = ClassesOf(output);
        bool square = classes.Contains("w-control");
        bool inverse = classes.Contains("text-on-inverse");
        bool icon = classes.Contains("text-icon");

        // Assert (Then)
        Assert.AreEqual(
            (true, true, false),
            (square, inverse, icon),
            "An icon button on an inverse surface should be square, in the surface's text color.");
    }

    /// <summary>
    /// Unit test to verify that a look the tag helper does not know is refused rather than drawn as some other look.
    /// </summary>
    [TestMethod]
    public void Process_UnknownLook_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        TagHelperOutput output = CreateOutput();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            Process("tertiary", output);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A look the tag helper does not know should be refused.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Makes a button as Razor hands it to the tag helper.
    /// </summary>
    /// <param name="attributes">The attributes the view wrote.</param>
    /// <returns>The button.</returns>
    private static TagHelperOutput CreateOutput(params TagHelperAttribute[] attributes)
    {
        return new TagHelperOutput(
            "button",
            new TagHelperAttributeList(attributes),
            static (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }

    /// <summary>
    /// Runs the tag helper on a button with a look, as Razor would.
    /// </summary>
    /// <param name="look">The look, as the view names it.</param>
    /// <param name="output">The button.</param>
    private static void Process(string look, TagHelperOutput output)
    {
        ButtonTagHelper helper = new ButtonTagHelper { Look = look };
        TagHelperContext context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            "button");
        helper.Process(context, output);
    }

    /// <summary>
    /// Reads the classes a button was given.
    /// </summary>
    /// <param name="output">The button.</param>
    /// <returns>Its classes, one to an entry.</returns>
    private static string[] ClassesOf(TagHelperOutput output)
    {
        TagHelperAttributeList attributes = output.Attributes;
        string value = attributes["class"]?.Value?.ToString() ?? string.Empty;
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    #endregion Private Methods
}
