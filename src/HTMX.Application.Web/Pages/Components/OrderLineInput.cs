namespace HTMX.Application.Web.Pages.Components;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// One line of the Order lines page's form: a product and how many of it. Its fields post with the line's position in
/// their names, such as <c>Input.Lines[1].Quantity</c>.
/// </summary>
public sealed class OrderLineInput
{
    /// <summary>
    /// Gets or sets the product's name, one of <see cref="OrderProducts.Names"/>, or <see langword="null"/> before one
    /// is chosen.
    /// </summary>
    [Display(Name = "Product")]
    [Required(ErrorMessage = "Choose a product.")]
    public string? Product { get; set; }

    /// <summary>
    /// Gets or sets how many of the product, or <see langword="null"/> before a number is entered.
    /// </summary>
    [Display(Name = "Quantity")]
    [Required(ErrorMessage = "Enter a quantity.")]
    [Range(1, 99, ErrorMessage = "Enter a quantity from 1 to 99.")]
    public int? Quantity { get; set; }
}
