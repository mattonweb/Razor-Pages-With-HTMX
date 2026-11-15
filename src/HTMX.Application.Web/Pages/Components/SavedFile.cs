namespace HTMX.Application.Web.Pages.Components;

/// <summary>
/// One file on the Confirmation page's list: its number and its name.
/// </summary>
public sealed record SavedFile
{
    /// <summary>
    /// Gets the file's number, from 10001, which its Delete names in its address.
    /// </summary>
    required public long Number { get; init; }

    /// <summary>
    /// Gets the file's name.
    /// </summary>
    required public string Name { get; init; }
}
