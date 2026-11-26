namespace HTMX.Application.Web.Pages.Components.Card;

using System;
using HTMX.Application.Web.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a card as the brand gives it, the surface every list, form and panel sits on: <c>card</c> on the card itself,
/// <c>card="header"</c> on its optional header, which holds its h2 title and at most one action, and
/// <c>card="body"</c> on its body. A card is a size container, so a component inside it adapts to the card's width,
/// at the card breakpoint, rather than to the window's, and cards in a column stand 16px apart. It sits beside the
/// Card page, which shows each part, so that the component is one folder; the base class it shares with the other
/// components stays in TagHelpers.
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class CardTagHelper : ComponentTagHelper
{
    private const string Name = "card";

    // A card that follows another card in a column stands 16px below it. The variant's selector is the utility's own
    // class twice, so it matches only a card after a card, and never adds to a gap the card's container already sets.
    private const string Card =
        "relative @container rounded-md border border-line bg-surface shadow-card [&+&]:mt-4";

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
