namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a filter box as the brand gives it, a search form that narrows a list as you type: <c>filter-box</c> on the
/// form with the search role, which holds the <c>filter</c> icon, drawn inside the search input, then the input, then
/// the Apply button for when the form cannot apply itself. It grows from 240px and stops at 360px.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole. The view hides the
/// Apply button with <c>htmx:hidden</c>, so the page says on the button itself when it shows. The form may be narrower
/// than its input's own width and the button together, so that on a narrow card the input gives up the room the button
/// takes rather than make the window scroll sideways.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class FilterBoxTagHelper : ComponentTagHelper
{
    private const string Name = "filter-box";

    private const string FilterBox =
        "relative flex max-w-[360px] min-w-0 flex-[1_1_240px] gap-2 [&>input]:min-w-0 [&>input]:flex-1 "
        + "[&>input]:pl-9 [&>svg]:pointer-events-none [&>svg]:absolute [&>svg]:top-1/2 [&>svg]:left-2.5 [&>svg]:size-[18px] "
        + "[&>svg]:-translate-y-1/2 [&>svg]:text-fg-muted";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                FilterBox,
            _ =>
                throw new InvalidOperationException(
                    $"A filter box has no parts, only the form itself, not \"{part}\"."),
        };
    }
}
