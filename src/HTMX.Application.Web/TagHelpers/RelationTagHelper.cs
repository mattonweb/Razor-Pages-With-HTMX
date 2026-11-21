namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws the box of a relation field as the brand gives it, another record as a value: <c>relation</c> on the element,
/// inside the field's fieldset, that holds the chosen record as a related-record link, or "Not set" in muted text, and
/// then the secondary button that opens the picker. The box looks like a control, and the button is joined to its right
/// end: no border of its own but a line at its left, square on the left and rounded on the right.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole. The button's
/// joined shape is written from the box, as <c>[&amp;&gt;button]:</c>, which outweighs the button's own look.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class RelationTagHelper : ComponentTagHelper
{
    private const string Name = "relation";

    private const string Relation =
        "flex min-h-control items-center gap-2 rounded-sm border border-line-input bg-surface pl-3 "
        + "[&>button]:ml-auto [&>button]:h-[calc(var(--control-height)_-_2px)] "
        + "[&>button]:rounded-[0_var(--radius-sm)_var(--radius-sm)_0] [&>button]:[border-width:0_0_0_1px]";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Relation,
            _ =>
                throw new InvalidOperationException(
                    $"A relation field's box has no parts, only the box itself, not \"{part}\"."),
        };
    }
}
