namespace HTMX.Application.Web.TagHelpers;

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws one of the brand's icons from the sprite: <c>&lt;icon name="search" /&gt;</c> becomes an <c>&lt;svg&gt;</c>
/// that uses the sprite's <c>search</c> symbol, hidden from assistive technology, since an icon only repeats the text
/// beside it or the label of the button it is in. It is drawn as the brand gives icons, 20px or, with
/// <c>size="sm"</c>, 16px, as a line in the color of the text around it; inside a button it is 18px, which the
/// button sets. Any class the view gives it is kept beside its own.
/// </summary>
/// <remarks>
/// <para>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole.
/// </para>
/// <para>
/// A tag helper's bound attributes are public settable properties because Razor sets them; that is the framework's
/// shape, as a page model's bound property is.
/// </para>
/// </remarks>
[HtmlTargetElement("icon", Attributes = NameAttributeName, TagStructure = TagStructure.WithoutEndTag)]
public sealed class IconTagHelper : TagHelper
{
    private const string NameAttributeName = "name";
    private const string SizeAttributeName = "size";

    // What every icon shares: no fill, and a line in the text's color with round ends and corners.
    private const string Shared =
        "flex-none fill-none stroke-current stroke-[1.75] [stroke-linecap:round] [stroke-linejoin:round]";

    // The two sizes: 20px, and 16px for size="sm".
    private const string Regular = "size-5";
    private const string Small = "size-4";

    /// <summary>
    /// Gets or sets the name of the sprite's symbol, such as <c>search</c>.
    /// </summary>
    [HtmlAttributeName(NameAttributeName)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the icon's size: <c>sm</c> for 16px, or nothing for the brand's 20px.
    /// </summary>
    [HtmlAttributeName(SizeAttributeName)]
    public string? Size { get; set; }

    /// <summary>
    /// Writes the icon as an <c>&lt;svg&gt;</c> that uses the sprite's symbol.
    /// </summary>
    /// <param name="context">The element as Razor read it.</param>
    /// <param name="output">The element as it will be written.</param>
    /// <exception cref="InvalidOperationException">The icon has no name, or its size is not <c>sm</c>.</exception>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (string.IsNullOrWhiteSpace(this.Name))
        {
            throw new InvalidOperationException("An icon names the sprite's symbol it draws.");
        }

        string classes = Shared + " " + SizeFor(this.Size);
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        foreach (string name in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            output.AddClass(name, HtmlEncoder.Default);
        }

        output.Attributes.SetAttribute("aria-hidden", "true");
        string symbol = HtmlEncoder.Default.Encode(this.Name);
        output.Content.SetHtmlContent($"<use href=\"/icons.svg#{symbol}\"></use>");
    }

    /// <summary>
    /// Gives the utility for an icon's size.
    /// </summary>
    /// <param name="size">The size, as the view names it.</param>
    /// <returns>The size's utility.</returns>
    /// <exception cref="InvalidOperationException">The size is not <c>sm</c>.</exception>
    private static string SizeFor(string? size)
    {
        return size switch
        {
            null =>
                Regular,
            "sm" =>
                Small,
            _ =>
                throw new InvalidOperationException($"An icon's size is sm, or none for 20px, not \"{size}\"."),
        };
    }
}
