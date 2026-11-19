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

    // What every badge shares: its size, shape and type.
    private const string Shared =
        "inline-grid h-[22px] min-w-8 place-items-center rounded-sm px-1.5 font-mono text-xs font-strong";

    private const string Version = Shared + " bg-brand-subtle text-brand-text";

    private const string Current = Shared + " bg-brand text-on-brand";

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
