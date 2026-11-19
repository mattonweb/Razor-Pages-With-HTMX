namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a busy bar as the brand gives it: <c>busy-bar</c> on an empty element along the bottom edge of a card's toolbar
/// or header. It shows nothing until it, or the card around it, waits for an answer, which HTMX marks
/// <c>.htmx-request</c> and a page without HTMX marks <c>aria-busy="true"</c>; then a segment crosses it every 1.1
/// seconds, more slowly when the user asks for less motion.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class BusyBarTagHelper : ComponentTagHelper
{
    private const string Name = "busy-bar";

    private const string BusyBar =
        "invisible relative z-1 -mt-0.5 h-0.5 overflow-hidden busy:visible in-busy:visible before:absolute "
        + "before:inset-y-0 before:left-0 before:w-[30%] before:animate-busy before:bg-accent "
        + "motion-reduce:before:[animation-duration:3s]";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                BusyBar,
            _ =>
                throw new InvalidOperationException(
                    $"A busy bar has no parts, only the bar itself, not \"{part}\"."),
        };
    }
}
