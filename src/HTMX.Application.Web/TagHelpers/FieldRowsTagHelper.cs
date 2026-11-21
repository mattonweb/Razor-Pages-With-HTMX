namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws rows as the brand gives them, several values of one kind that the user writes, such as telephone numbers:
/// <c>field-rows</c> on the list inside the field's fieldset, with an item for each value that holds its control,
/// filling the row, and its Remove button. The items take their look from the list, so the view writes them as plain
/// HTML.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole. The attribute is
/// not <c>rows</c>, which a textarea already has.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class FieldRowsTagHelper : ComponentTagHelper
{
    private const string Name = "field-rows";

    private const string FieldRows =
        "m-0 grid list-none gap-2 p-0 [&>li]:flex [&>li]:items-center [&>li]:gap-2 "
        + "[&>li_:is(input,select)]:min-w-0 [&>li_:is(input,select)]:flex-1";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                FieldRows,
            _ =>
                throw new InvalidOperationException(
                    $"Rows have no parts, only the list itself, not \"{part}\"."),
        };
    }
}
