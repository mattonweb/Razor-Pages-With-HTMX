namespace HTMX.Application.Web.Pages.Contacts;

/// <summary>
/// What the <c>_PhoneNumberField</c> partial shows: a row for each telephone number the form holds, and the buttons
/// that add a row and take one away, each of which posts the form for the field to be drawn again.
/// </summary>
/// <remarks>
/// The page draws the field inside the form, and Add and Remove answer HTMX with the field alone, so one partial draws
/// both answers. Without HTMX the same buttons post the form and get the whole page back.
/// </remarks>
public sealed record PhoneNumberField
{
    /// <summary>
    /// Gets the rows, in order; there is always at least one, empty when the contact has no number.
    /// </summary>
    required public PhoneNumberRow[] Rows { get; init; }

    /// <summary>
    /// Gets the address Add posts the form to, which answers with one more row.
    /// </summary>
    required public string AddAction { get; init; }

    /// <summary>
    /// Gets the address each row's Remove posts the form to, with the row's position, which answers without that row.
    /// </summary>
    required public string RemoveAction { get; init; }

    /// <summary>
    /// Gets a value indicating whether there is room for another row; there is not once the form holds the most a
    /// contact can have.
    /// </summary>
    required public bool CanAdd { get; init; }

    /// <summary>
    /// Gets the numbers as another save left them, when a save that overtook this one left different ones, or
    /// <see langword="null"/>.
    /// </summary>
    public string? Saved { get; init; }

    /// <summary>
    /// Gets why a save was refused for the numbers, or <see langword="null"/>. It belongs to the whole field, since the
    /// rows share one name; the rows whose number is wrong are marked too.
    /// </summary>
    public string? Error { get; init; }
}
