namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a toolbar as the brand gives it, the row at the top of a list's card: <c>toolbar</c> on the row, which holds a
/// filter box at the left and a segmented control, a navigation landmark, at the right end. It wraps onto two lines on
/// a narrow card.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class ToolbarTagHelper : ComponentTagHelper
{
    private const string Name = "toolbar";

    private const string Toolbar =
        "flex flex-wrap items-center gap-3 border-b border-line px-4 py-3 [&>nav]:ml-auto";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Toolbar,
            _ =>
                throw new InvalidOperationException(
                    $"A toolbar has no parts, only the row itself, not \"{part}\"."),
        };
    }
}
