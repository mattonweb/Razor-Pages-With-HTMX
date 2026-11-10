namespace HTMX.Application.Web.Pages.Users;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// A user's values as a form posts them, and the rules each must pass, written to be shared with a form that creates
/// users, so that both ask the same of every value.
/// </summary>
public class UserInput
{
    /// <summary>
    /// Gets or sets the person's name, as the application shows it.
    /// </summary>
    [Display(Name = "Name")]
    [Required(ErrorMessage = "Enter a name.")]
    [StringLength(100, ErrorMessage = "Keep the name to 100 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name the user signs in with. Space around it is allowed, because the form trims every value
    /// before it is saved; space inside it is not.
    /// </summary>
    [Display(Name = "Username")]
    [Required(ErrorMessage = "Enter a username.")]
    [StringLength(50, ErrorMessage = "Keep the username to 50 characters or fewer.")]
    [RegularExpression(
        @"^\s*[A-Za-z0-9._-]+\s*$",
        ErrorMessage = "Use only letters, digits, dots, hyphens and underscores, with no spaces.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Display(Name = "Email")]
    [Required(ErrorMessage = "Enter an email address.")]
    [EmailAddress(ErrorMessage = "Enter an email address, such as pat@example.com.")]
    [StringLength(200, ErrorMessage = "Keep the email address to 200 characters or fewer.")]
    public string Email { get; set; } = string.Empty;
}
