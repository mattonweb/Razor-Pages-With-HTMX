namespace HTMX.Test.Unit.Application.Web.Pages.Components.Icon;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HTMX.Application.Web.Pages.Components.Icon;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="IconTagHelper"/> class: the <c>&lt;svg&gt;</c> it
/// writes for a symbol of the sprite, its two sizes, and that an icon with no name or an unknown size is refused.
/// </summary>
[TestClass]
public sealed class IconTagHelperTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that an icon is written as an <c>&lt;svg&gt;</c>, hidden from assistive technology, that
    /// uses the sprite's symbol, at 20px, keeping the class the view gave it.
    /// </summary>
    [TestMethod]
    public void Process_Name_WritesAnSvgThatUsesTheSymbol()
    {
        // Arrange (Given)
        TagHelperOutput output = CreateOutput(new TagHelperAttribute("class", "text-icon"));

        // Act (When)
        Process("search", null, output);
        TagHelperAttributeList attributes = output.Attributes;
        string? hidden = attributes["aria-hidden"]?.Value?.ToString();
        string content = output.Content.GetContent();
        string[] classes = ClassesOf(output);

        // Assert (Then)
        Assert.AreEqual(
            ("svg", "true", "<use href=\"/icons.svg#search\"></use>"),
            (output.TagName, hidden, content),
            "The icon should be a hidden <svg> that uses the sprite's symbol.");
        Assert.AreEqual(
            (true, true, true),
            (classes.Contains("size-5"), classes.Contains("stroke-current"), classes.Contains("text-icon")),
            "The icon should be 20px, drawn in the text's color, and keep the view's class.");
    }

    /// <summary>
    /// Unit test to verify that a small icon is 16px, and not 20px as well.
    /// </summary>
    [TestMethod]
    public void Process_SmallSize_IsSixteenPixels()
    {
        // Arrange (Given)
        TagHelperOutput output = CreateOutput();

        // Act (When)
        Process("filter", "sm", output);
        string[] classes = ClassesOf(output);

        // Assert (Then)
        Assert.AreEqual(
            (true, false),
            (classes.Contains("size-4"), classes.Contains("size-5")),
            "A small icon should be 16px only.");
    }

    /// <summary>
    /// Unit test to verify that an icon with no name is refused rather than drawn empty.
    /// </summary>
    [TestMethod]
    public void Process_NoName_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        TagHelperOutput output = CreateOutput();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            Process(" ", null, output);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "An icon with no name should be refused.");
    }

    /// <summary>
    /// Unit test to verify that a size the tag helper does not know is refused rather than drawn at 20px.
    /// </summary>
    [TestMethod]
    public void Process_UnknownSize_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        TagHelperOutput output = CreateOutput();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            Process("filter", "lg", output);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A size the tag helper does not know should be refused.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Makes an <c>&lt;icon&gt;</c> element as Razor hands it to the tag helper.
    /// </summary>
    /// <param name="attributes">The attributes the view wrote.</param>
    /// <returns>The element.</returns>
    private static TagHelperOutput CreateOutput(params TagHelperAttribute[] attributes)
    {
        return new TagHelperOutput(
            "icon",
            new TagHelperAttributeList(attributes),
            static (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }

    /// <summary>
    /// Runs the tag helper on an <c>&lt;icon&gt;</c> element, as Razor would.
    /// </summary>
    /// <param name="name">The symbol's name.</param>
    /// <param name="size">The size, or <see langword="null"/> for none.</param>
    /// <param name="output">The element.</param>
    private static void Process(string name, string? size, TagHelperOutput output)
    {
        IconTagHelper helper = new IconTagHelper { Name = name, Size = size };
        TagHelperContext context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            "icon");
        helper.Process(context, output);
    }

    /// <summary>
    /// Reads the classes an icon was given.
    /// </summary>
    /// <param name="output">The icon.</param>
    /// <returns>Its classes, one to an entry.</returns>
    private static string[] ClassesOf(TagHelperOutput output)
    {
        TagHelperAttributeList attributes = output.Attributes;
        string value = attributes["class"]?.Value?.ToString() ?? string.Empty;
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    #endregion Private Methods
}
