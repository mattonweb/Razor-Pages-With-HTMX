namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a version badge as the brand gives it, a version number in the brand violet such as "v4": <c>version</c> on
/// the badge, or <c>version="current"</c>, filled, for the version a record is at now.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole. A view that
/// chooses at run time writes an empty value for a badge that is not the current version, since a null one would
/// leave the attribute, and so the badge's look, out.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class VersionTagHelper : ComponentTagHelper
{
    private const string Name = "version";

    // What every badge shares: its size, shape and type, and in forced colors, where its fill would take the window's
    // color, a border to keep its shape.
    private const string Shared =
        "inline-grid h-[22px] min-w-8 place-items-center rounded-sm px-1.5 font-mono text-xs font-strong "
        + "forced-colors:border";

    private const string Version = Shared + " bg-brand-subtle text-brand-text";

    // The current version, filled, and in forced colors in the theme's highlight, by name. The browser would draw a
    // plate in the window's color behind its text, hiding it, unless the badge adjusts no colors.
    private const string Current =
        Shared + " bg-brand text-on-brand forced-colors:bg-[color:Highlight] forced-colors:text-[color:HighlightText] "
        + "forced-colors:forced-color-adjust-none";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Version,
            "current" =>
                Current,
            _ =>
                throw new InvalidOperationException(
                    $"A version badge is current, or nothing for any other version, not \"{part}\"."),
        };
    }
}
