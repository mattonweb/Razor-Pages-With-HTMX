namespace HTMX.Application.Web.TagHelpers;

using System;
using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws one of the brand's components from Tailwind utilities, part by part. The view names the component in an
/// attribute on the element that is the part: with no value it is the component itself, as in
/// <c>&lt;section card&gt;</c>, and a value names one of its parts or variants, as in
/// <c>&lt;header card="header"&gt;</c>. The tag helper takes the attribute away and gives the element the part's
/// utilities, keeping any class the view wrote beside them; the element, its ARIA and its <c>hx-</c> attributes stay
/// as the view wrote them.
/// </summary>
/// <remarks>
/// <para>
/// An attribute whose value is <see langword="null"/>, as in <c>grid="@(archived ? "archived" : null)"</c>, is left
/// out of the page by Razor, and the element gets nothing.
/// </para>
/// <para>
/// The attribute is read rather than bound to a property, because Razor accepts an attribute with no value only when
/// it is bound to a <see cref="bool"/>.
/// </para>
/// </remarks>
public abstract class ComponentTagHelper : TagHelper
{
    /// <summary>
    /// Gets the attribute that names the component, such as <c>card</c>.
    /// </summary>
    protected abstract string AttributeName { get; }

    /// <summary>
    /// Gives the element the utilities of the part its attribute names.
    /// </summary>
    /// <param name="context">The element as Razor read it.</param>
    /// <param name="output">The element as it will be written.</param>
    /// <exception cref="InvalidOperationException">The attribute names a part the component does not have.</exception>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);
        output.Attributes.RemoveAll(this.AttributeName);
        if (!context.AllAttributes.TryGetAttribute(this.AttributeName, out TagHelperAttribute? attribute))
        {
            return;
        }

        string? value = Convert.ToString(attribute.Value, CultureInfo.InvariantCulture);
        string? part = string.IsNullOrEmpty(value) ? null : value;
        foreach (string name in this.ClassesFor(part).Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            output.AddClass(name, HtmlEncoder.Default);
        }
    }

    /// <summary>
    /// Gives the utilities of one part of the component.
    /// </summary>
    /// <param name="part">
    /// The part, as the view names it, or <see langword="null"/> for the component itself, when the attribute has no
    /// value or an empty one.
    /// </param>
    /// <returns>The part's utilities.</returns>
    /// <exception cref="InvalidOperationException">The component has no such part.</exception>
    protected abstract string ClassesFor(string? part);
}
