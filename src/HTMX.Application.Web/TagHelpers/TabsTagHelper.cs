namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws tabs as the brand gives them, links to the sections of one page: <c>tabs</c> on the labelled navigation
/// landmark, <c>tabs="tab"</c> on each link, whose section is current when it says <c>aria-current="page"</c>, and
/// <c>tabs="count"</c> on an optional count after a link's label.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole. The current tab
/// keeps its accent under the pointer.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class TabsTagHelper : ComponentTagHelper
{
    private const string Name = "tabs";

    private const string Tabs = "mb-4 flex flex-wrap gap-1 border-b border-line";

    private const string Tab =
        "-mb-px inline-flex items-center gap-2 border-b-2 border-transparent px-3 py-2 font-strong text-fg-muted "
        + "hover:no-underline not-aria-[current=page]:hover:text-fg aria-[current=page]:border-accent "
        + "aria-[current=page]:text-accent";

    private const string Count =
        "min-w-5 rounded-full bg-neutral-subtle px-1.5 text-center text-xs text-fg-muted tabular-nums";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Tabs,
            "tab" =>
                Tab,
            "count" =>
                Count,
            _ =>
                throw new InvalidOperationException(
                    $"Tabs' parts are tab and count, or none for the landmark itself, not \"{part}\"."),
        };
    }
}
