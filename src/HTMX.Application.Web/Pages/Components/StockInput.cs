namespace HTMX.Application.Web.Pages.Components;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// What one row of the Stock table posts: how many of its item are on hand.
/// </summary>
public sealed class StockInput
{
    /// <summary>
    /// Gets or sets how many are on hand, from 0 to 999.
    /// </summary>
    [Display(Name = "On hand")]
    [Required(ErrorMessage = "Enter how many are on hand.")]
    [Range(0, 999, ErrorMessage = "Enter a count from 0 to 999.")]
    public int? OnHand { get; set; }
}
