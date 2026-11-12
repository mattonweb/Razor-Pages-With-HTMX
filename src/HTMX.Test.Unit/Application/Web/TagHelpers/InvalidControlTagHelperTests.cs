namespace HTMX.Test.Unit.Application.Web.TagHelpers;

using System.Collections.Generic;
using System.Threading.Tasks;
using HTMX.Application.Web.TagHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="InvalidControlTagHelper"/> class: what an invalid
/// control is given, and that a valid one is left alone.
/// </summary>
[TestClass]
public sealed class InvalidControlTagHelperTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that an invalid control is marked invalid, names its message after the hint it already
    /// names, and takes the focus.
    /// </summary>
    [TestMethod]
    public void Process_InvalidField_MarksTheControl()
    {
        // Arrange (Given)
        ViewContext view = CreateView();
        view.ViewData.ModelState.AddModelError("Input.Website", "Enter a full web address.");
        TagHelperOutput output = CreateOutput(new TagHelperAttribute("aria-describedby", "website-hint"));

        // Act (When)
        Process(view, "Input.Website", output);
        TagHelperAttributeList attributes = output.Attributes;

        // Assert (Then)
        Assert.AreEqual(
            "true",
            attributes["aria-invalid"]?.Value,
            "An invalid control should say so.");
        Assert.AreEqual(
            "website-hint Input_Website-error",
            attributes["aria-describedby"]?.Value?.ToString(),
            "The control should name its message after its hint.");
        Assert.IsTrue(
            attributes.ContainsName("autofocus"),
            "The first invalid control should take the focus.");
    }

    /// <summary>
    /// Unit test to verify that only the first invalid control in a response takes the focus.
    /// </summary>
    [TestMethod]
    public void Process_SecondInvalidField_LeavesTheFocusToTheFirst()
    {
        // Arrange (Given)
        ViewContext view = CreateView();
        view.ViewData.ModelState.AddModelError("Input.Name", "Enter a name.");
        view.ViewData.ModelState.AddModelError("Input.Website", "Enter a full web address.");
        TagHelperOutput first = CreateOutput();
        TagHelperOutput second = CreateOutput();

        // Act (When)
        Process(view, "Input.Name", first);
        Process(view, "Input.Website", second);

        // Assert (Then)
        Assert.IsTrue(
            first.Attributes.ContainsName("autofocus"),
            "The first invalid control should take the focus.");
        Assert.IsFalse(
            second.Attributes.ContainsName("autofocus"),
            "The second invalid control should leave it to the first.");
        Assert.AreEqual(
            "Input_Website-error",
            second.Attributes["aria-describedby"]?.Value?.ToString(),
            "The second control should still name its message.");
    }

    /// <summary>
    /// Unit test to verify that in a response that leaves the focus alone, such as a check made as the user types, an
    /// invalid control is still marked invalid but takes no focus.
    /// </summary>
    [TestMethod]
    public void Process_ResponseLeavesFocusAlone_MarksTheControlWithoutFocus()
    {
        // Arrange (Given)
        ViewContext view = CreateView();
        view.ViewData.ModelState.AddModelError("Input.Name", "Another organization already has this name.");
        TagHelperOutput output = CreateOutput();
        InvalidControlTagHelper.LeaveFocusAlone(view.HttpContext);

        // Act (When)
        Process(view, "Input.Name", output);
        TagHelperAttributeList attributes = output.Attributes;

        // Assert (Then)
        Assert.AreEqual(
            "true",
            attributes["aria-invalid"]?.Value,
            "An invalid control should still say so.");
        Assert.IsFalse(
            attributes.ContainsName("autofocus"),
            "No control should take the focus in a response that leaves it alone.");
    }

    /// <summary>
    /// Unit test to verify that a valid control is left as the framework wrote it.
    /// </summary>
    [TestMethod]
    public void Process_ValidField_LeavesTheControlAlone()
    {
        // Arrange (Given)
        ViewContext view = CreateView();
        view.ViewData.ModelState.AddModelError("Input.Website", "Enter a full web address.");
        TagHelperOutput output = CreateOutput();

        // Act (When)
        Process(view, "Input.Name", output);

        // Assert (Then)
        Assert.IsEmpty(
            output.Attributes,
            "A valid control should gain no attributes.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the view being rendered, with its own request and no errors yet. A test adds errors to its view data's
    /// ModelState, which is the one the framework's tag helpers and this one read.
    /// </summary>
    /// <returns>The view.</returns>
    private static ViewContext CreateView()
    {
        EmptyModelMetadataProvider metadata = new EmptyModelMetadataProvider();
        return new ViewContext
        {
            HttpContext = new DefaultHttpContext(),
            ViewData = new ViewDataDictionary(metadata, new ModelStateDictionary()),
        };
    }

    /// <summary>
    /// Creates a control as the framework's tag helper left it, with the attributes given.
    /// </summary>
    /// <param name="attributes">The attributes already written.</param>
    /// <returns>The control.</returns>
    private static TagHelperOutput CreateOutput(params TagHelperAttribute[] attributes)
    {
        return new TagHelperOutput(
            "input",
            new TagHelperAttributeList(attributes),
            static (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }

    /// <summary>
    /// Runs the tag helper on a control for a field, as Razor would.
    /// </summary>
    /// <param name="view">The view being rendered.</param>
    /// <param name="field">The field's name, as <c>asp-for</c> gives it.</param>
    /// <param name="output">The control.</param>
    private static void Process(ViewContext view, string field, TagHelperOutput output)
    {
        EmptyModelMetadataProvider metadata = new EmptyModelMetadataProvider();
        ModelExplorer explorer = metadata.GetModelExplorerForType(typeof(string), model: null);
        InvalidControlTagHelper helper = new InvalidControlTagHelper
        {
            For = new ModelExpression(field, explorer),
            ViewContext = view,
        };
        TagHelperContext context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            "control");
        helper.Process(context, output);
    }

    #endregion Private Methods
}
