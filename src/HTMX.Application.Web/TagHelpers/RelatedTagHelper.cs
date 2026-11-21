namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a related-record link as the brand gives it, another record named by a link led by its type's icon:
/// <c>related</c> on the link, or <c>related="archived"</c>, muted, for a record that is archived, which the view
/// follows with a neutral Archived chip 8px after it. The view gives the icon its small size.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole. A view that
/// chooses at run time writes an empty value for a record that is not archived, since a null one would leave the
/// attribute, and so the link's look, out.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class RelatedTagHelper : ComponentTagHelper
{
    private const string Name = "related";

    private const string Related = "inline-flex items-center gap-1";

    private const string Archived = Related + " text-fg-muted";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Related,
            "archived" =>
                Archived,
            _ =>
                throw new InvalidOperationException(
                    $"A related-record link is archived, or nothing for any other record, not \"{part}\"."),
        };
    }
}
