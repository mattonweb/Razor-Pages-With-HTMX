namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws an empty state as the brand gives it, what a list shows when it has nothing to show: <c>empty-state</c> on an
/// element that holds one icon, one heading, one sentence saying why, and at most one action, a link drawn as a
/// button. They take their look from it, so the view writes them as plain HTML.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class EmptyStateTagHelper : ComponentTagHelper
{
    private const string Name = "empty-state";

    private const string EmptyState =
        "grid justify-items-center gap-2 px-4 py-8 text-center [&>a]:mt-2 [&>p]:text-fg-muted [&>svg]:size-10 "
        + "[&>svg]:stroke-[1.5] [&>svg]:text-fg-disabled";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                EmptyState,
            _ =>
                throw new InvalidOperationException(
                    $"An empty state has no parts, only the element itself, not \"{part}\"."),
        };
    }
}
