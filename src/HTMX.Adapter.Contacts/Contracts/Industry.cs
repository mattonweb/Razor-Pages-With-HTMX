namespace HTMX.Adapter.Contacts.Contracts;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// The industry an organization works in, chosen from a short fixed list. Each member's display name is the text a
/// page shows and a select lists.
/// </summary>
public enum Industry
{
    /// <summary>
    /// Making goods.
    /// </summary>
    [Display(Name = "Manufacturing")]
    Manufacturing,

    /// <summary>
    /// Producing and selling food and drink.
    /// </summary>
    [Display(Name = "Food and drink")]
    FoodAndDrink,

    /// <summary>
    /// Publishing, film, broadcasting and design.
    /// </summary>
    [Display(Name = "Media")]
    Media,

    /// <summary>
    /// Selling goods to the public.
    /// </summary>
    [Display(Name = "Retail")]
    Retail,

    /// <summary>
    /// Moving and storing goods.
    /// </summary>
    [Display(Name = "Shipping")]
    Shipping,

    /// <summary>
    /// Software, hardware and the services around them.
    /// </summary>
    [Display(Name = "Technology")]
    Technology,
}
