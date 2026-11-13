namespace HTMX.Application.Web.Pages.Components;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

/// <summary>
/// The File field page's form as it posts: a title, and the file the form sends.
/// </summary>
public sealed class FileFieldInput
{
    /// <summary>
    /// Gets or sets the title the file is sent under.
    /// </summary>
    [Display(Name = "Title")]
    [Required(ErrorMessage = "Enter a title.")]
    [StringLength(100, ErrorMessage = "Keep the title to 100 characters or fewer.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file, or <see langword="null"/> when none was chosen. A browser sends an empty part for a file
    /// input left empty, which binds as none.
    /// </summary>
    [Display(Name = "File")]
    [Required(ErrorMessage = "Choose a file.")]
    public IFormFile? File { get; set; }
}
