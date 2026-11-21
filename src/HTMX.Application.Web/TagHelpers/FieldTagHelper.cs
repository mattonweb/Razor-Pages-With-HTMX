namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a field as the brand gives it, one value in a form: <c>field</c> on the element that holds the label, the
/// control at the field's full width, an optional hint and the validation message; <c>field="hint"</c> on the hint;
/// <c>field="message"</c> on the message, which takes no space while it is empty; and <c>field="summary"</c> on the
/// summary of errors that belong to no single field, at the top of a form, which takes no space while there are none.
/// The label of a field whose control is required is followed by a red asterisk. A group of controls, such as rows or
/// a relation field, is a fieldset that says <c>field</c>, whose legend looks like a label, 4px above what it names.
/// </summary>
/// <remarks>
/// <para>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </para>
/// <para>
/// A control is required when it says <c>aria-required="true"</c>, or carries the <c>data-val-required</c> attribute
/// that the framework's tag helpers write for <c>[Required]</c>. The framework's validation summary tag helper marks a
/// summary with no errors <c>.validation-summary-valid</c>, and that is how the summary knows to hide. An invalid
/// control's own look is an element default, in Styles/base.css.
/// </para>
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class FieldTagHelper : ComponentTagHelper
{
    private const string Name = "field";

    private const string Field =
        "grid min-w-0 content-start gap-1 [&>:is(label,legend)]:text-sm [&>:is(label,legend)]:font-strong "
        + "[&>:is(label,legend)]:text-fg [&>legend]:mb-1 "
        + "[&_:is(input:not([type=checkbox],[type=radio]),select,textarea)]:w-full "
        + "has-[:is([aria-required=true],[data-val-required])]:[&>:is(label,legend)]:after:text-danger "
        + "has-[:is([aria-required=true],[data-val-required])]:[&>:is(label,legend)]:after:content-['_*']";

    private const string Hint = "text-sm text-fg-muted";

    private const string Message = "text-sm text-danger empty:hidden";

    private const string Summary =
        "mb-4 rounded-md border border-danger-line bg-danger-subtle px-4 py-3 text-danger [&_ul]:m-0 "
        + "[&_ul]:list-disc [&_ul]:pl-5 [&.validation-summary-valid]:hidden";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Field,
            "hint" =>
                Hint,
            "message" =>
                Message,
            "summary" =>
                Summary,
            _ =>
                throw new InvalidOperationException(
                    $"A field's parts are hint, message and summary, or none for the field itself, not \"{part}\"."),
        };
    }
}
