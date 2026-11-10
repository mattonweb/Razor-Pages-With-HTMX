namespace HTMX.Application.Web.Pages.Contacts;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// A contact's values as a form posts them, and the rules each must pass, shared by the forms that create and edit
/// contacts so that both ask the same of every value.
/// </summary>
public class ContactInput
{
    /// <summary>
    /// Gets or sets the person's name.
    /// </summary>
    [Display(Name = "Name")]
    [Required(ErrorMessage = "Enter a name.")]
    [StringLength(100, ErrorMessage = "Keep the name to 100 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the person's job title, or <see langword="null"/> when the field is left empty.
    /// </summary>
    [Display(Name = "Job title")]
    [StringLength(100, ErrorMessage = "Keep the job title to 100 characters or fewer.")]
    public string? JobTitle { get; set; }

    /// <summary>
    /// Gets or sets the person's email address, or <see langword="null"/> when the field is left empty.
    /// </summary>
    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Enter an email address, such as pat@example.com.")]
    [StringLength(200, ErrorMessage = "Keep the email address to 200 characters or fewer.")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the person's telephone number, or <see langword="null"/> when the field is left empty.
    /// </summary>
    [Display(Name = "Phone")]
    [Phone(ErrorMessage = "Enter a telephone number, such as +1 555-0142.")]
    [StringLength(40, ErrorMessage = "Keep the telephone number to 40 characters or fewer.")]
    public string? Phone { get; set; }

    /// <summary>
    /// Gets or sets free text about the person, or <see langword="null"/> when the field is left empty.
    /// </summary>
    [Display(Name = "Notes")]
    [StringLength(2000, ErrorMessage = "Keep the notes to 2,000 characters or fewer.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the organization the person belongs to, or <see langword="null"/> before one
    /// is chosen.
    /// </summary>
    [Display(Name = "Organization")]
    [Required(ErrorMessage = "Choose an organization.")]
    public long? OrganizationId { get; set; }
}
