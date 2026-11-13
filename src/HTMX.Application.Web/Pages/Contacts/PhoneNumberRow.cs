namespace HTMX.Application.Web.Pages.Contacts;

/// <summary>
/// One row of the telephone number field: the number as the form holds it, and what the row needs to be drawn.
/// </summary>
public sealed record PhoneNumberRow
{
    /// <summary>
    /// Gets the number as written, or <see langword="null"/> for a row left empty.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Gets the row's position, from 0, which its Remove button posts and its label counts from 1.
    /// </summary>
    required public int Position { get; init; }

    /// <summary>
    /// Gets a value indicating whether the row's field takes the keyboard: the row Add just made, or the row that took
    /// the place of the one just removed.
    /// </summary>
    required public bool Focused { get; init; }

    /// <summary>
    /// Gets a value indicating whether a refused save found the row's number wrong.
    /// </summary>
    required public bool Invalid { get; init; }
}
