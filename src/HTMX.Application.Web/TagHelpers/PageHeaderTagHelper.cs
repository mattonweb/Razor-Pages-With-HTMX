namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws the page header as the brand gives it, the top of the main area: <c>page-header</c> on the header itself,
/// which holds the breadcrumbs, the title row and an optional muted line; <c>page-header="title-row"</c> on the row
/// that holds the h1 at the left and the page's actions at the right; <c>page-header="title"</c> on a record's title,
/// its name beside its status chip and version badge; and <c>page-header="actions"</c> on the page's actions, the one
/// primary action last.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class PageHeaderTagHelper : ComponentTagHelper
{
    private const string Name = "page-header";

    private const string Header = "mb-6 grid gap-2";

    private const string TitleRow = "flex flex-wrap items-center justify-between gap-3";

    private const string Title = "flex flex-wrap items-center gap-3";

    private const string Actions = "inline-flex flex-wrap items-center gap-2";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Header,
            "title-row" =>
                TitleRow,
            "title" =>
                Title,
            "actions" =>
                Actions,
            _ =>
                throw new InvalidOperationException(
                    "A page header's parts are title-row, title and actions, or none for the header itself, "
                    + $"not \"{part}\"."),
        };
    }
}
