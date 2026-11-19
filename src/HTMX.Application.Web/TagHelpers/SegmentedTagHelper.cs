namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a segmented control as the brand gives it, a few views of the same content: <c>segmented</c> on the labelled
/// navigation landmark, the track; <c>segmented="segment"</c> on each link, the current one saying
/// <c>aria-current="page"</c>; and <c>segmented="count"</c> on an optional count after a segment's label.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class SegmentedTagHelper : ComponentTagHelper
{
    private const string Name = "segmented";

    private const string Track = "inline-flex gap-0.5 rounded-sm bg-neutral-subtle p-0.5";

    private const string Segment =
        "inline-flex h-7 items-center gap-2 rounded-[3px] px-3 font-strong text-fg-muted hover:text-fg "
        + "hover:no-underline aria-[current=page]:bg-surface aria-[current=page]:text-fg "
        + "aria-[current=page]:shadow-card";

    private const string Count = "text-xs font-regular tabular-nums";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Track,
            "segment" =>
                Segment,
            "count" =>
                Count,
            _ =>
                throw new InvalidOperationException(
                    "A segmented control's parts are segment and count, or none for the track itself, "
                    + $"not \"{part}\"."),
        };
    }
}
