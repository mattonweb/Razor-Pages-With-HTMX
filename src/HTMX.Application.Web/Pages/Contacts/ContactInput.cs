namespace HTMX.Application.Web.Pages.Contacts;

using System.ComponentModel.DataAnnotations;
using HTMX.Application.Web.Validation;

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
    /// Gets or sets the person's telephone numbers as the form's rows post them, in order, a row left empty as
    /// <see langword="null"/>. Every row's field has the same name, <c>Input.PhoneNumbers</c>, so the rows bind to this
    /// array with no index numbers.
    /// </summary>
    [Display(Name = "Phone numbers")]
    [TelephoneNumbers]
    public string?[] PhoneNumbers { get; set; } = [];

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
