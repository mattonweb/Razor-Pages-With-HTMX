namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a card as the brand gives it, the surface every list, form and panel sits on: <c>card</c> on the card itself,
/// <c>card="header"</c> on its optional header, which holds its h2 title and at most one action, and
/// <c>card="body"</c> on its body. A card is a size container, so a component inside it adapts to the card's width,
/// at the card breakpoint, rather than to the window's.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class CardTagHelper : ComponentTagHelper
{
    private const string Name = "card";

    private const string Card = "relative @container rounded-md border border-line bg-surface shadow-card";

    private const string Header =
        "flex items-center justify-between gap-3 border-b border-line px-4 py-3 [&>h2]:text-lg";

    private const string Body = "p-4";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Card,
            "header" =>
                Header,
            "body" =>
                Body,
            _ =>
                throw new InvalidOperationException(
                    $"A card's parts are header and body, or none for the card itself, not \"{part}\"."),
        };
    }
}
