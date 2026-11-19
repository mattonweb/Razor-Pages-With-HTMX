namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a menu as the brand gives it, a short list of actions on one thing: <c>menu</c> on the popover, which the
/// browser places 4px below the button that opens it, right edges aligned, or above it when there is no room below;
/// <c>menu="item"</c> on each link or button in it; and <c>menu="danger"</c> on an action that cannot be undone, which
/// comes last. A plain <c>&lt;hr&gt;</c> separates groups.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole. A browser that
/// cannot place the menu by its button shows it in the middle of the window.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class MenuTagHelper : ComponentTagHelper
{
    private const string Name = "menu";

    private const string Menu =
        "inset-auto mx-0 mt-1 mb-0 min-w-[180px] rounded-md border border-line bg-surface p-1 text-fg shadow-popup "
        + "[position-area:bottom_span-left] [position-try-fallbacks:flip-block] "
        + "not-supports-[position-area:bottom]:inset-0 not-supports-[position-area:bottom]:m-auto "
        + "not-supports-[position-area:bottom]:h-fit [&>hr]:my-1 [&>hr]:border-t [&>hr]:border-line";

    // What every item shares: a row the menu's width, its 18px icon, and the wash under the pointer.
    private const string Shared =
        "flex h-control w-full cursor-pointer items-center gap-2 rounded-sm px-2 text-left hover:bg-surface-hover "
        + "hover:no-underline [&>svg]:size-[18px]";

    private const string Item = Shared + " text-fg [&>svg]:text-icon";

    private const string Danger = Shared + " text-danger [&>svg]:text-danger";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Menu,
            "item" =>
                Item,
            "danger" =>
                Danger,
            _ =>
                throw new InvalidOperationException(
                    $"A menu's parts are item and danger, or none for the menu itself, not \"{part}\"."),
        };
    }
}
