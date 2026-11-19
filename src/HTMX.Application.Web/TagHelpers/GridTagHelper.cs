namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a grid as the brand gives it, a table of records: <c>grid</c> on the table, and on its parts:
/// <list type="bullet">
/// <item><description><c>scroll</c>, the element around the table, which scrolls sideways when the card is too
/// narrow;</description></item>
/// <item><description><c>sort</c>, a sortable column's header link, whose arrow shows on the sorted column, the one
/// whose header says <c>aria-sort</c>, and under the pointer or the keyboard;</description></item>
/// <item><description><c>title</c>, the title cell: a link to the record and an optional second line;</description></item>
/// <item><description><c>number</c>, a header or cell in a column of numbers;</description></item>
/// <item><description><c>actions</c>, the last cell, which holds the row's menu button;</description></item>
/// <item><description><c>archived</c>, a row whose record is archived, among rows that are not.</description></item>
/// </list>
/// The header and body cells take their look from the table, so the view writes them as plain HTML, and the rows fade
/// while the card around the table waits for an answer.
/// </summary>
/// <remarks>
/// <para>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </para>
/// <para>
/// The cells' look is written inside <c>:where()</c>, which weighs nothing, so that a part on a cell, such as
/// <c>number</c>'s alignment, wins over it; Tailwind's typography plugin keeps its styles light the same way. The
/// title link's color is a custom property, <c>--grid-title</c>, which an archived row sets for its cells.
/// </para>
/// <para>
/// The table's head is positioned, so that a visually hidden label in a header cell, such as the actions column's,
/// which is positioned absolutely, stays inside the scroll box: placed by the card instead, it stood past the table's
/// right edge and made the window scroll sideways on a narrow screen.
/// </para>
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class GridTagHelper : ComponentTagHelper
{
    private const string Name = "grid";

    private const string Grid =
        "w-full border-collapse [--grid-title:var(--accent)] [&>thead]:relative "
        + "[:where(&_th)]:h-10 [:where(&_th)]:border-b [:where(&_th)]:border-line [:where(&_th)]:px-4 "
        + "[:where(&_th)]:text-left [:where(&_th)]:text-sm [:where(&_th)]:font-strong [:where(&_th)]:whitespace-nowrap "
        + "[:where(&_th)]:text-fg-muted "
        + "[:where(&_td)]:h-row [:where(&_td)]:border-b [:where(&_td)]:border-line [:where(&_td)]:px-4 "
        + "[:where(&_td)]:py-2 [:where(&_td)]:align-middle "
        + "[&_tbody_tr:last-child_td]:border-b-0 [&_tbody_tr:hover]:bg-surface-hover "
        + "in-busy:[&>tbody]:opacity-50 in-busy:[&>tbody]:transition-opacity in-busy:[&>tbody]:duration-200 "
        + "in-busy:[&>tbody]:ease-[ease]";

    private const string Scroll = "overflow-x-auto";

    private const string Sort =
        "inline-flex items-center gap-1 text-inherit hover:text-fg hover:no-underline [&>svg]:opacity-0 "
        + "hover:[&>svg]:opacity-100 focus-visible:[&>svg]:opacity-100 [[aria-sort]>&]:text-fg "
        + "[[aria-sort]>&>svg]:opacity-100";

    private const string Title =
        "[&>a]:block [&>a]:font-strong [&>a]:text-(--grid-title) [&>label]:block [&>label]:cursor-pointer "
        + "[&>label]:font-strong [&>span]:block [&>span]:text-sm [&>span]:text-fg-muted";

    private const string Number = "text-right tabular-nums";

    private const string Actions = "w-[1%] py-0 text-right whitespace-nowrap";

    private const string Archived = "text-fg-muted [--grid-title:var(--text-muted)]";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Grid,
            "scroll" =>
                Scroll,
            "sort" =>
                Sort,
            "title" =>
                Title,
            "number" =>
                Number,
            "actions" =>
                Actions,
            "archived" =>
                Archived,
            _ =>
                throw new InvalidOperationException(
                    "A grid's parts are scroll, sort, title, number, actions and archived, or none for the table "
                    + $"itself, not \"{part}\"."),
        };
    }
}
