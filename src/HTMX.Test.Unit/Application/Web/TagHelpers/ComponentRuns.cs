namespace HTMX.Test.Unit.Application.Web.TagHelpers;

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HTMX.Application.Web.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Runs a component's tag helper on an element as Razor would, and reads the classes it gave the element, for the tests
/// of <see cref="ComponentTagHelper"/> and of each component drawn with it, wherever that component's folder is.
/// </summary>
internal static class ComponentRuns
{
    /// <summary>
    /// Runs a tag helper on an element, as Razor would.
    /// </summary>
    /// <param name="helper">The tag helper.</param>
    /// <param name="attribute">The component's attribute, as the view wrote it.</param>
    /// <param name="others">The element's other attributes.</param>
    /// <returns>The element as it will be written.</returns>
    internal static TagHelperOutput Process(
        ComponentTagHelper helper,
        TagHelperAttribute attribute,
        params TagHelperAttribute[] others)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(attribute);
        TagHelperAttributeList attributes = new TagHelperAttributeList(others) { attribute };
        TagHelperContext context = new TagHelperContext(
            attributes,
            new Dictionary<object, object>(),
            attribute.Name);
        TagHelperOutput output = CreateOutput(new TagHelperAttributeList(attributes));
        helper.Process(context, output);
        return output;
    }

    /// <summary>
    /// Makes an element as Razor hands it to a tag helper.
    /// </summary>
    /// <param name="attributes">The attributes the view wrote.</param>
    /// <returns>The element.</returns>
    internal static TagHelperOutput CreateOutput(TagHelperAttributeList attributes)
    {
        return new TagHelperOutput(
            "div",
            attributes,
            static (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }

    /// <summary>
    /// Reads the classes an element was given, as the browser reads them: the tag helper writes each one HTML-encoded,
    /// so a utility such as <c>[&amp;>svg]:size-10</c> arrives with its ampersand encoded.
    /// </summary>
    /// <param name="output">The element.</param>
    /// <returns>Its classes, one to an entry.</returns>
    internal static string[] ClassesOf(TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        TagHelperAttributeList attributes = output.Attributes;
        string value = WebUtility.HtmlDecode(attributes["class"]?.Value?.ToString()) ?? string.Empty;
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
