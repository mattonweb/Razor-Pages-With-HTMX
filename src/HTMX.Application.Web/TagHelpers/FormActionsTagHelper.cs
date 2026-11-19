namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws the form action bar as the brand gives it, the last thing in a form, sticking to the bottom of the window
/// while a long form scrolls: <c>form-actions</c> on the bar, which holds any actions on the whole record at the left,
/// and <c>form-actions="end"</c> on the group at the right end that holds Cancel and the one primary action.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class FormActionsTagHelper : ComponentTagHelper
{
    private const string Name = "form-actions";

    private const string FormActions =
        "sticky bottom-4 z-5 mt-4 flex flex-wrap items-center gap-2 rounded-md border border-line bg-surface px-4 "
        + "py-3 shadow-popup";

    private const string End = "ml-auto flex flex-wrap gap-2";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                FormActions,
            "end" =>
                End,
            _ =>
                throw new InvalidOperationException(
                    $"A form action bar's one part is end, or none for the bar itself, not \"{part}\"."),
        };
    }
}
