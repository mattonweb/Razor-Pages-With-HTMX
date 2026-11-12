namespace HTMX.Test.Unit.Application.Web.Pages;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HTMX.Application.Web.TagHelpers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws one control as a page's view would once its handler has run, over the handler's ModelState and in the same
/// request, so a page test sees whether its answer would give that control the keyboard.
/// </summary>
internal static class DrawnControls
{
    /// <summary>
    /// Says whether a field's control, drawn in the page's answer, would take <c>autofocus</c>.
    /// </summary>
    /// <param name="page">The page, after its handler has run.</param>
    /// <param name="field">The field's name, as <c>asp-for</c> gives it, such as <c>Input.Name</c>.</param>
    /// <returns><see langword="true"/> when the control would take the focus.</returns>
    internal static bool TakesFocus(PageModel page, string field)
    {
        ArgumentNullException.ThrowIfNull(page);
        EmptyModelMetadataProvider metadata = new EmptyModelMetadataProvider();
        ViewContext view = new ViewContext
        {
            HttpContext = page.HttpContext,
            ViewData = new ViewDataDictionary(metadata, page.ModelState),
        };
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
        TagHelperOutput output = new TagHelperOutput(
            "input",
            new TagHelperAttributeList(),
            static (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        helper.Process(context, output);
        return output.Attributes.ContainsName("autofocus");
    }
}
