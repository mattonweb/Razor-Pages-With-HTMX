namespace HTMX.Application.Web.Pages;

using System;

/// <summary>
/// What a form's field says under its control after another save overtook the one being made: the value that save
/// left, when it differs from the one being edited, so the person can compare them before saving again.
/// </summary>
public static class SavedValue
{
    /// <summary>
    /// Gives the saved value to show under a field, or nothing when it is the value being edited. Values are compared
    /// as the form saves them, trimmed, with an empty value as none.
    /// </summary>
    /// <param name="saved">The value the other save left, if any.</param>
    /// <param name="edited">The value being edited, if any.</param>
    /// <param name="none">What to show when the other save left no value, such as "no job title".</param>
    /// <returns>The value to show, or <see langword="null"/> when there is nothing to compare.</returns>
    public static string? Hint(string? saved, string? edited, string none)
    {
        string savedText = saved?.Trim() ?? string.Empty;
        string editedText = edited?.Trim() ?? string.Empty;
        if (string.Equals(savedText, editedText, StringComparison.Ordinal))
        {
            return null;
        }

        return savedText.Length == 0 ? none : savedText;
    }
}
