namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a pager as the brand gives it, the foot of a list's card, in three columns: <c>pager</c> on the foot itself;
/// <c>pager="summary"</c> on the range shown, at the left; <c>pager="pages"</c> on Previous and Next, in the middle;
/// and <c>pager="size"</c> on the Rows per page form, at the right. On a card narrower than 560px the range and Rows
/// per page share the first line and Previous and Next take the second, centered.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole. The view hides the
/// Rows per page form's Apply button with <c>htmx:hidden</c>, as the filter box's.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class PagerTagHelper : ComponentTagHelper
{
    private const string Name = "pager";

    private const string Pager =
        "grid grid-cols-[1fr_auto_1fr] items-center gap-3 border-t border-line px-4 py-3 text-sm "
        + "[grid-template-areas:'summary_pages_size'] @max-card:grid-cols-[1fr_auto] "
        + "@max-card:[grid-template-areas:'summary_size'_'pages_pages']";

    private const string Summary = "text-fg-muted [grid-area:summary]";

    private const string Pages = "flex gap-2 [grid-area:pages] @max-card:justify-self-center";

    private const string Size =
        "flex items-center gap-2 justify-self-end text-fg-muted [grid-area:size] [&>select]:tabular-nums";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Pager,
            "summary" =>
                Summary,
            "pages" =>
                Pages,
            "size" =>
                Size,
            _ =>
                throw new InvalidOperationException(
                    $"A pager's parts are summary, pages and size, or none for the foot itself, not \"{part}\"."),
        };
    }
}
