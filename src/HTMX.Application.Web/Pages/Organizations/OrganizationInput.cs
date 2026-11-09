namespace HTMX.Application.Web.Pages.Organizations;

using System.ComponentModel.DataAnnotations;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Validation;

/// <summary>
/// An organization's values as a form posts them, and the rules each must pass, shared by the create and edit forms
/// so that both ask the same of every value.
/// </summary>
public class OrganizationInput
{
    /// <summary>
    /// Gets or sets the organization's name.
    /// </summary>
    [Display(Name = "Name")]
    [Required(ErrorMessage = "Enter a name.")]
    [StringLength(100, ErrorMessage = "Keep the name to 100 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the industry the organization works in, or <see langword="null"/> before one is chosen.
    /// </summary>
    [Display(Name = "Industry")]
    [Required(ErrorMessage = "Choose an industry.")]
    public Industry? Industry { get; set; }

    /// <summary>
    /// Gets or sets the organization's website, or <see langword="null"/> when the field is left empty.
    /// </summary>
    [Display(Name = "Website")]
    [AbsoluteWebAddress(ErrorMessage = "Enter a full web address, such as https://placeholder.example.")]
    [StringLength(200, ErrorMessage = "Keep the web address to 200 characters or fewer.")]
    public string? Website { get; set; }
}
