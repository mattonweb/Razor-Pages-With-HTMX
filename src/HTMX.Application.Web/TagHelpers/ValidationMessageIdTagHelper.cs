namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Gives a field's validation message an <c>id</c>, such as <c>Input_Name-error</c>, so that its control can name it in
/// <c>aria-describedby</c>; <see cref="InvalidControlTagHelper"/> does the naming. It runs beside the framework's own
/// tag helper for <c>asp-validation-for</c>, which writes the message itself.
/// </summary>
/// <remarks>
/// A tag helper's bound attributes are public settable properties because Razor sets them; that is the framework's
/// shape, as a page model's bound property is.
/// </remarks>
[HtmlTargetElement("span", Attributes = ForAttributeName)]
public sealed class ValidationMessageIdTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-validation-for";

    /// <summary>
    /// Gets or sets the field the message is for.
    /// </summary>
    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression? For { get; set; }

    /// <summary>
    /// Gets or sets the view being rendered, which Razor supplies.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext? ViewContext { get; set; }

    /// <summary>
    /// Gives the <c>id</c> of a field's validation message.
    /// </summary>
    /// <param name="fullName">The field's full name, as its control is named in the form, such as <c>Input.Name</c>.</param>
    /// <returns>The <c>id</c>, built as the framework builds a control's <c>id</c> from its name, then
    /// <c>-error</c>.</returns>
    public static string IdFor(string fullName)
    {
        return TagBuilder.CreateSanitizedId(fullName, "_") + "-error";
    }

    /// <summary>
    /// Writes the message's <c>id</c>.
    /// </summary>
    /// <param name="context">The element as Razor read it.</param>
    /// <param name="output">The element as it will be written.</param>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (this.For is null || this.ViewContext is null)
        {
            return;
        }

        ViewDataDictionary viewData = this.ViewContext.ViewData;
        TemplateInfo template = viewData.TemplateInfo;
        string fullName = template.GetFullHtmlFieldName(this.For.Name);
        output.Attributes.SetAttribute("id", IdFor(fullName));
    }
}
