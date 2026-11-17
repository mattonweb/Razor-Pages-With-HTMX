namespace HTMX.Application.Web.TagHelpers;

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a chip as the brand gives it, a status label with a dot and never a button, from the tone its <c>chip</c>
/// attribute names: <c>neutral</c>, the default; <c>success</c>; <c>danger</c>; <c>warning</c>; <c>info</c>; or
/// <c>brand</c>. The tone's Tailwind utilities are written here once rather than on every chip, and any class the view
/// gives the element is kept beside them.
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
[HtmlTargetElement("span", Attributes = ToneAttributeName)]
public sealed class ChipTagHelper : TagHelper
{
    private const string ToneAttributeName = "chip";

    // What every tone shares: the shape, the type, and the dot before the label, in the label's color.
    private const string Shared =
        "inline-flex h-6 items-center gap-1.5 whitespace-nowrap rounded-full border px-2.5 text-xs font-strong "
        + "before:size-2 before:rounded-[50%] before:bg-current";

    // The tones: a line, a subtle fill and a text color each.
    private const string Neutral = "border-neutral-line bg-neutral-subtle text-neutral";
    private const string Success = "border-success-line bg-success-subtle text-success";
    private const string Danger = "border-danger-line bg-danger-subtle text-danger";
    private const string Warning = "border-warning-line bg-warning-subtle text-warning";
    private const string Info = "border-info-line bg-info-subtle text-info";
    private const string Brand = "border-brand-subtle bg-brand-subtle text-brand-text";

    /// <summary>
    /// Gets or sets the chip's tone: <c>neutral</c>, <c>success</c>, <c>danger</c>, <c>warning</c>, <c>info</c> or
    /// <c>brand</c>.
    /// </summary>
    [HtmlAttributeName(ToneAttributeName)]
    public string? Tone { get; set; }

    /// <summary>
    /// Gives the element the utilities of its tone.
    /// </summary>
    /// <param name="context">The element as Razor read it.</param>
    /// <param name="output">The element as it will be written.</param>
    /// <exception cref="InvalidOperationException">The tone is not one of the six.</exception>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        string classes = Shared + " " + ClassesFor(this.Tone);
        foreach (string name in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            output.AddClass(name, HtmlEncoder.Default);
        }
    }

    /// <summary>
    /// Gives the utilities that belong to one tone alone.
    /// </summary>
    /// <param name="tone">The tone, as the view names it.</param>
    /// <returns>The tone's utilities.</returns>
    /// <exception cref="InvalidOperationException">The tone is not one of the six.</exception>
    private static string ClassesFor(string? tone)
    {
        return tone switch
        {
            "neutral" =>
                Neutral,
            "success" =>
                Success,
            "danger" =>
                Danger,
            "warning" =>
                Warning,
            "info" =>
                Info,
            "brand" =>
                Brand,
            _ =>
                throw new InvalidOperationException(
                    $"A chip's tone is neutral, success, danger, warning, info or brand, not \"{tone}\"."),
        };
    }
}
