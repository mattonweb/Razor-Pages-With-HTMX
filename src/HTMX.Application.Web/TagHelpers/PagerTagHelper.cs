namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a pager as the brand gives it, in three columns: <c>pager</c> on the pager itself, at the foot of a list's
/// card, or <c>pager="head"</c> on one at the head of the card, between its header and a list that grows as its last
/// row comes into view; <c>pager="summary"</c> on the range shown, at the left; <c>pager="pages"</c> on Previous and
/// Next, in the middle; and <c>pager="size"</c> on the Rows per page form, at the right. On a card narrower than 560px
/// the range and Rows per page share the first line and Previous and Next take the second, centered.
/// </summary>
/// <remarks>
/// A list that grows has Load more in place of Previous and Next and no Rows per page. Its view writes Load more into
/// the pager with no part, so it takes the first free place: the middle, or on a narrow card the right of the first
/// line. A list that stops short of every record says so in <c>pager="pages"</c> instead.
/// <para>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole. The view hides the
/// Rows per page form's Apply button with <c>htmx:hidden</c>, as the filter box's.
/// </para>
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class PagerTagHelper : ComponentTagHelper
{
    private const string Name = "pager";

    // The pager's columns, the same at the foot of a card and at its head. On a narrow card the middle part's own
    // margin sets it 12px below the first line, rather than a gap between the rows, so that a pager with nothing in
    // the middle, as a list that grows has, ends with its first line.
    private const string Columns =
        "grid grid-cols-[1fr_auto_1fr] items-center gap-x-3 border-line px-4 py-3 text-sm "
        + "[grid-template-areas:'summary_pages_size'] @max-card:grid-cols-[1fr_auto] "
        + "@max-card:[grid-template-areas:'summary_size'_'pages_pages']";

    // At the foot, the pager's border is above it, between it and the list; at the head, below it.
    private const string Foot = "border-t " + Columns;

    private const string Head = "border-b " + Columns;

    private const string Summary = "text-fg-muted [grid-area:summary]";

    private const string Pages = "flex gap-2 [grid-area:pages] @max-card:mt-3 @max-card:justify-self-center";

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
                Foot,
            "head" =>
                Head,
            "summary" =>
                Summary,
            "pages" =>
                Pages,
            "size" =>
                Size,
            _ =>
                throw new InvalidOperationException(
                    "A pager's parts are summary, pages and size, or head for a pager at the head of its card, or "
                    + $"none for one at the foot, not \"{part}\"."),
        };
    }
}
