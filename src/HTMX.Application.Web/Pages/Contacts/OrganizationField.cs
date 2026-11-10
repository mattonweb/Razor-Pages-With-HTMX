namespace HTMX.Application.Web.Pages.Contacts;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What the <c>_OrganizationField</c> partial shows: the brand's relation field for a contact's organization, the
/// chosen organization as a link, and the button that opens the picker.
/// </summary>
public sealed record OrganizationField
{
    /// <summary>
    /// Gets the form's values. The partial names its hidden input <c>Input.OrganizationId</c>, as the page binds it.
    /// </summary>
    required public ContactInput Input { get; init; }

    /// <summary>
    /// Gets the chosen organization, or <see langword="null"/> before one is chosen.
    /// </summary>
    public OrganizationReference? Chosen { get; init; }

    /// <summary>
    /// Gets the organization another save left the contact in, to show under the field, or <see langword="null"/>
    /// when no save overtook this one or it left the same organization.
    /// </summary>
    public string? Saved { get; init; }

    /// <summary>
    /// Gets a value indicating whether the field's button takes the focus when it is drawn: after a choice in the
    /// picker, whose button had it, and not when the page first loads.
    /// </summary>
    public bool Focus { get; init; }
}
