namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws breadcrumbs as the brand gives them, where a page sits: <c>breadcrumbs</c> on the navigation landmark
/// labelled "Breadcrumb", which holds an ordered list of links. A "/" goes before each item after the first, and the
/// last item, the current page, is text marked <c>aria-current="page"</c>. The list and its items take their look from
/// the landmark, so the view writes them as plain HTML.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class BreadcrumbsTagHelper : ComponentTagHelper
{
    private const string Name = "breadcrumbs";

    private const string Breadcrumbs =
        "[&>ol]:m-0 [&>ol]:flex [&>ol]:list-none [&>ol]:flex-wrap [&>ol]:gap-2 [&>ol]:p-0 [&>ol]:text-sm "
        + "[&>ol]:text-fg-muted [&_li+li]:before:mr-2 [&_li+li]:before:text-fg-disabled "
        + "[&_li+li]:before:content-['/'] [&_[aria-current=page]]:text-fg";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Breadcrumbs,
            _ =>
                throw new InvalidOperationException(
                    $"Breadcrumbs have no parts, only the landmark itself, not \"{part}\"."),
        };
    }
}
