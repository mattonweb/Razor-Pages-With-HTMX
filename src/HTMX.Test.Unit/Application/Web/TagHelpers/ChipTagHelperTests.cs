namespace HTMX.Test.Unit.Application.Web.TagHelpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HTMX.Application.Web.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="ChipTagHelper"/> class: the utilities each tone
/// gives a chip, that a class the view wrote is kept, and that a tone it does not know is refused.
/// </summary>
[TestClass]
public sealed class ChipTagHelperTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a success chip gets the utilities every chip shares and the success tone's own, and
    /// keeps the class the view gave it.
    /// </summary>
    [TestMethod]
    public void Process_SuccessTone_AddsTheSharedAndSuccessUtilities()
    {
        // Arrange (Given)
        TagHelperOutput output = CreateOutput(new TagHelperAttribute("class", "ml-2"));

        // Act (When)
        Process("success", output);
        string[] classes = ClassesOf(output);
        bool shared = classes.Contains("rounded-full");
        bool success = classes.Contains("bg-success-subtle");
        bool kept = classes.Contains("ml-2");
        bool neutral = classes.Contains("bg-neutral-subtle");

        // Assert (Then)
        Assert.AreEqual(
            (true, true, true, false),
            (shared, success, kept, neutral),
            "A success chip should have the shared utilities, the success fill and the view's class alone.");
    }

    /// <summary>
    /// Unit test to verify that a tone the tag helper does not know is refused rather than drawn as some other tone.
    /// </summary>
    [TestMethod]
    public void Process_UnknownTone_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        TagHelperOutput output = CreateOutput();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            Process("critical", output);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A tone the tag helper does not know should be refused.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Makes a chip as Razor hands it to the tag helper.
    /// </summary>
    /// <param name="attributes">The attributes the view wrote.</param>
    /// <returns>The chip.</returns>
    private static TagHelperOutput CreateOutput(params TagHelperAttribute[] attributes)
    {
        return new TagHelperOutput(
            "span",
            new TagHelperAttributeList(attributes),
            static (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }

    /// <summary>
    /// Runs the tag helper on a chip with a tone, as Razor would.
    /// </summary>
    /// <param name="tone">The tone, as the view names it.</param>
    /// <param name="output">The chip.</param>
    private static void Process(string tone, TagHelperOutput output)
    {
        ChipTagHelper helper = new ChipTagHelper { Tone = tone };
        TagHelperContext context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            "chip");
        helper.Process(context, output);
    }

    /// <summary>
    /// Reads the classes a chip was given.
    /// </summary>
    /// <param name="output">The chip.</param>
    /// <returns>Its classes, one to an entry.</returns>
    private static string[] ClassesOf(TagHelperOutput output)
    {
        TagHelperAttributeList attributes = output.Attributes;
        string value = attributes["class"]?.Value?.ToString() ?? string.Empty;
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    #endregion Private Methods
}
