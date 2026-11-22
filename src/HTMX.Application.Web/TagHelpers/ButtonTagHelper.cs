namespace HTMX.Application.Web.TagHelpers;

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a button as the brand gives it, from the look its <c>button</c> attribute names: <c>secondary</c>, the
/// default; <c>primary</c>; <c>danger</c>; <c>ghost</c>; or <c>icon</c>, a square ghost button that holds an icon alone
/// and carries an <c>aria-label</c>. On an inverse surface, such as a toast, <c>ghost-inverse</c> and
/// <c>icon-inverse</c> are the ghost and icon looks in its colors. It draws a <c>&lt;button&gt;</c>, a link that looks like a button, or a
/// <c>&lt;span&gt;</c> that stands in for a link with nowhere to go. The look's Tailwind utilities are written here
/// once rather than on every button, and any class the view gives the element is kept beside them.
/// </summary>
/// <remarks>
/// <para>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole, never built from
/// parts. A disabled button, or one marked <c>aria-disabled="true"</c>, does not change under the pointer. A button
/// waiting for the answer to the request it sent, which HTMX marks <c>.htmx-request</c>, or one marked
/// <c>aria-busy="true"</c>, keeps its width, hides its label and shows a spinner, which turns more slowly when the
/// user asks for less motion.
/// </para>
/// <para>
/// A tag helper's bound attributes are public settable properties because Razor sets them; that is the framework's
/// shape, as a page model's bound property is.
/// </para>
/// </remarks>
[HtmlTargetElement("button", Attributes = LookAttributeName)]
[HtmlTargetElement("a", Attributes = LookAttributeName)]
[HtmlTargetElement("span", Attributes = LookAttributeName)]
public sealed class ButtonTagHelper : TagHelper
{
    private const string LookAttributeName = "button";

    // What every look shares: the shape and the type, an icon inside, and the disabled and busy states. In forced
    // colors, which draw every text and line in the theme's colors, a disabled button is the theme's gray, and a busy
    // one hides its label in the button's face and turns a gray ring with a highlighted top, all named as the theme's
    // own colors, which the browser leaves as they are.
    private const string Shared =
        "inline-flex h-control cursor-pointer items-center justify-center gap-2 whitespace-nowrap rounded-sm border "
        + "font-strong no-underline hover:no-underline [&_svg]:size-[18px] "
        + "disabled:cursor-not-allowed disabled:border-neutral-subtle disabled:bg-neutral-subtle "
        + "disabled:text-fg-disabled aria-disabled:cursor-not-allowed aria-disabled:border-neutral-subtle "
        + "aria-disabled:bg-neutral-subtle aria-disabled:text-fg-disabled "
        + "busy:pointer-events-none busy:relative busy:text-transparent busy:after:absolute busy:after:size-4 "
        + "busy:after:animate-spin busy:after:rounded-[50%] busy:after:border-2 "
        + "motion-reduce:busy:after:[animation-duration:2s] "
        + "forced-colors:disabled:border-[color:GrayText] forced-colors:disabled:text-[color:GrayText] "
        + "forced-colors:aria-disabled:border-[color:GrayText] forced-colors:aria-disabled:text-[color:GrayText] "
        + "forced-colors:busy:text-[color:ButtonFace] forced-colors:busy:after:border-[color:GrayText] "
        + "forced-colors:busy:after:border-t-[color:Highlight]";

    // The spinner on a light face: a ring in the line color with its top in the accent.
    private const string LightSpinner = "busy:after:border-line busy:after:border-t-accent";

    // The looks: the padding, the colors, and what the pointer and the spinner do on each.
    private const string Secondary =
        "px-3 border-line-input bg-surface text-fg hover:bg-surface-hover " + LightSpinner;

    private const string Primary =
        "px-3 border-accent-fill bg-accent-fill text-on-accent hover:border-accent-fill-hover "
        + "hover:bg-accent-fill-hover active:bg-accent-fill-active "
        + "busy:after:border-[color-mix(in_srgb,var(--on-accent)_40%,transparent)] busy:after:border-t-on-accent";

    private const string Danger =
        "px-3 border-danger-fill bg-danger-fill text-on-accent hover:border-danger-fill-hover "
        + "hover:bg-danger-fill-hover " + LightSpinner;

    private const string Ghost =
        "px-3 border-transparent bg-transparent text-icon hover:bg-surface-hover " + LightSpinner;

    private const string Icon =
        "w-control px-0 border-transparent bg-transparent text-icon hover:bg-surface-hover " + LightSpinner;

    // On an inverse surface: the accent or the surface's own text, washed with that text under the pointer.
    private const string GhostInverse =
        "px-3 border-transparent bg-transparent text-accent-on-inverse "
        + "hover:bg-[color-mix(in_srgb,var(--on-inverse)_12%,transparent)] " + LightSpinner;

    private const string IconInverse =
        "w-control px-0 border-transparent bg-transparent text-on-inverse "
        + "hover:bg-[color-mix(in_srgb,var(--on-inverse)_12%,transparent)] " + LightSpinner;

    /// <summary>
    /// Gets or sets the button's look: <c>secondary</c>, <c>primary</c>, <c>danger</c>, <c>ghost</c> or <c>icon</c>,
    /// or on an inverse surface <c>ghost-inverse</c> or <c>icon-inverse</c>.
    /// </summary>
    [HtmlAttributeName(LookAttributeName)]
    public string? Look { get; set; }

    /// <summary>
    /// Gives the element the utilities of its look.
    /// </summary>
    /// <param name="context">The element as Razor read it.</param>
    /// <param name="output">The element as it will be written.</param>
    /// <exception cref="InvalidOperationException">The look is not one of the seven.</exception>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        string classes = Shared + " " + ClassesFor(this.Look);
        foreach (string name in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            output.AddClass(name, HtmlEncoder.Default);
        }
    }

    /// <summary>
    /// Gives the utilities that belong to one look alone.
    /// </summary>
    /// <param name="look">The look, as the view names it.</param>
    /// <returns>The look's utilities.</returns>
    /// <exception cref="InvalidOperationException">The look is not one of the seven.</exception>
    private static string ClassesFor(string? look)
    {
        return look switch
        {
            "secondary" =>
                Secondary,
            "primary" =>
                Primary,
            "danger" =>
                Danger,
            "ghost" =>
                Ghost,
            "icon" =>
                Icon,
            "ghost-inverse" =>
                GhostInverse,
            "icon-inverse" =>
                IconInverse,
            _ =>
                throw new InvalidOperationException(
                    "A button's look is secondary, primary, danger, ghost, icon, ghost-inverse or icon-inverse, "
                    + $"not \"{look}\"."),
        };
    }
}
