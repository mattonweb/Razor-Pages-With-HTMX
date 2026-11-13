namespace HTMX.Application.Web.Pages.Components;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// The Reload on event page's form as it posts: one note's text.
/// </summary>
public sealed class NoteInput
{
    /// <summary>
    /// Gets or sets the note's text.
    /// </summary>
    [Display(Name = "Note")]
    [Required(ErrorMessage = "Write a note.")]
    [StringLength(200, ErrorMessage = "Keep the note to 200 characters or fewer.")]
    public string Text { get; set; } = string.Empty;
}
