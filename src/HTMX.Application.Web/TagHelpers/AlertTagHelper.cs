namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws an alert as the brand gives it, a banner about a state that lasts: <c>alert</c> on the banner, in the neutral
/// tone, or <c>alert="info"</c>, <c>"success"</c>, <c>"warning"</c> or <c>"danger"</c> for another; then
/// <c>alert="content"</c> on its title and text, and <c>alert="actions"</c> on its one or two buttons. Its icon, first,
/// takes the tone's color; the text keeps the page's.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class AlertTagHelper : ComponentTagHelper
{
    private const string Name = "alert";

    // What every tone shares: the banner's shape, 16px above the content, and its icon 1px lower than the text's top.
    private const string Shared = "mb-4 flex flex-wrap items-start gap-3 rounded-md border px-4 py-3 [&>svg]:mt-px";

    // The tones: a line, a subtle fill and the icon's color each.
    private const string Neutral = Shared + " border-neutral-line bg-neutral-subtle [&>svg]:text-neutral";
    private const string Info = Shared + " border-info-line bg-info-subtle [&>svg]:text-info";
    private const string Success = Shared + " border-success-line bg-success-subtle [&>svg]:text-success";
    private const string Warning = Shared + " border-warning-line bg-warning-subtle [&>svg]:text-warning";
    private const string Danger = Shared + " border-danger-line bg-danger-subtle [&>svg]:text-danger";

    private const string Content = "grid flex-[1_1_280px] gap-0.5";

    private const string Actions = "flex flex-wrap gap-2";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null or "neutral" =>
                Neutral,
            "info" =>
                Info,
            "success" =>
                Success,
            "warning" =>
                Warning,
            "danger" =>
                Danger,
            "content" =>
                Content,
            "actions" =>
                Actions,
            _ =>
                throw new InvalidOperationException(
                    "An alert's tones are neutral, info, success, warning and danger, and its parts content and "
                    + $"actions, not \"{part}\"."),
        };
    }
}
