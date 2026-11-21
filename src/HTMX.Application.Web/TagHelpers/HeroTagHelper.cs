namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a hero as the brand gives it, the opening of a landing page, inside a card: <c>hero</c> on the element that
/// holds the illustration beside the text; <c>hero="art"</c> on the illustration, 180px square; <c>hero="text"</c> on
/// the text, an h2 and a muted paragraph that says what the application is for; and <c>hero="actions"</c> on the row
/// of the page's main actions, the primary one first. On a card narrower than 560px the illustration shrinks to 120px
/// and moves above the text.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class HeroTagHelper : ComponentTagHelper
{
    private const string Name = "hero";

    private const string Hero =
        "flex items-center gap-6 p-6 @max-card:flex-col @max-card:items-start @max-card:gap-4 @max-card:p-4";

    private const string Art = "size-[180px] flex-none @max-card:size-[120px]";

    private const string Text = "grid min-w-0 justify-items-start gap-3 [&>p]:text-fg-muted";

    private const string Actions = "mt-2 inline-flex flex-wrap items-center gap-2";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Hero,
            "art" =>
                Art,
            "text" =>
                Text,
            "actions" =>
                Actions,
            _ =>
                throw new InvalidOperationException(
                    $"A hero's parts are art, text and actions, or none for the hero itself, not \"{part}\"."),
        };
    }
}
