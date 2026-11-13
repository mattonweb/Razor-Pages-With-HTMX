namespace HTMX.Application.Web.TagHelpers;

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Marks a control whose value failed validation the way the brand asks: <c>aria-invalid="true"</c>, and its message
/// named in <c>aria-describedby</c>, after any hint already named there. The first invalid control in the response
/// also gets <c>autofocus</c>, which a browser follows when it loads the page and HTMX follows after a swap, so the
/// cursor lands on the first thing to fix. The framework's own tag helpers write the control and its
/// <c>input-validation-error</c> class; they run at the order -1000, and this one at the default, 0, after them.
/// </summary>
/// <remarks>
/// A tag helper's bound attributes are public settable properties because Razor sets them; that is the framework's
/// shape, as a page model's bound property is.
/// </remarks>
[HtmlTargetElement("input", Attributes = ForAttributeName)]
[HtmlTargetElement("select", Attributes = ForAttributeName)]
[HtmlTargetElement("textarea", Attributes = ForAttributeName)]
public sealed class InvalidControlTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-for";
    private const string DescribedBy = "aria-describedby";

    // Marks the response once a control has taken autofocus, so that no other control does.
    private static readonly object FocusTaken = new object();

    /// <summary>
    /// Gets or sets the field the control is for.
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
    /// Says that a response moves no focus, so that no invalid control in it takes <c>autofocus</c>. A field checked
    /// while the user types answers this way: by the time the answer arrives the user may have moved on, and the
    /// keyboard stays where they are.
    /// </summary>
    /// <param name="context">The request the response answers.</param>
    public static void LeaveFocusAlone(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Items.TryAdd(FocusTaken, true);
    }

    /// <summary>
    /// Claims the response's one <c>autofocus</c> for an invalid control that is not drawn with <c>asp-for</c>, such as
    /// one of several rows that share a name. Only the first invalid control in the response gets it, as here.
    /// </summary>
    /// <param name="context">The request the response answers.</param>
    /// <returns><see langword="true"/> when no control had taken it, and the caller now has.</returns>
    public static bool TryTakeFocus(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryAdd(FocusTaken, true);
    }

    /// <summary>
    /// Marks the control when its field has a validation error, and leaves it alone otherwise.
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
        ModelStateDictionary modelState = viewData.ModelState;
        if (modelState.GetFieldValidationState(fullName) != ModelValidationState.Invalid)
        {
            return;
        }

        TagHelperAttributeList attributes = output.Attributes;
        attributes.SetAttribute("aria-invalid", "true");
        string messageId = ValidationMessageIdTagHelper.IdFor(fullName);
        string describedBy = attributes.TryGetAttribute(DescribedBy, out TagHelperAttribute? hint)
            ? $"{hint.Value} {messageId}"
            : messageId;
        attributes.SetAttribute(DescribedBy, describedBy);
        HttpContext http = this.ViewContext.HttpContext;
        TakeFocus(http.Items, attributes);
    }

    /// <summary>
    /// Gives the control <c>autofocus</c>, unless an earlier control in the same response already has it.
    /// </summary>
    /// <param name="items">The response's own store, which says whether a control has already taken it.</param>
    /// <param name="attributes">The control's attributes.</param>
    private static void TakeFocus(IDictionary<object, object?> items, TagHelperAttributeList attributes)
    {
        if (!items.TryAdd(FocusTaken, true))
        {
            return;
        }

        attributes.SetAttribute(new TagHelperAttribute("autofocus"));
    }
}
