namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws fields as the brand gives them, label-over-value pairs in as many columns at least 260px wide as fit:
/// <c>fields</c> on a description list whose pairs are each grouped in a <c>&lt;div&gt;</c>, or on a form's card body,
/// whose fields then fill the same columns. The labels and values take their look from it, so the view writes them as
/// plain HTML.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class FieldsTagHelper : ComponentTagHelper
{
    private const string Name = "fields";

    private const string Fields =
        "m-0 grid grid-cols-[repeat(auto-fill,minmax(min(260px,100%),1fr))] gap-x-6 gap-y-4 [&_dd]:mt-0.5 "
        + "[&_dd]:wrap-anywhere [&_dt]:text-sm [&_dt]:text-fg-muted";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Fields,
            _ =>
                throw new InvalidOperationException(
                    $"Fields have no parts, only the list itself, not \"{part}\"."),
        };
    }
}
