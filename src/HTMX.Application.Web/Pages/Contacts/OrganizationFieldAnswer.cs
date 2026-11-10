namespace HTMX.Application.Web.Pages.Contacts;

/// <summary>
/// What the <c>_OrganizationFieldAnswer</c> partial shows: the relation field, and the picker that goes beside it.
/// </summary>
public sealed record OrganizationFieldAnswer
{
    /// <summary>
    /// Gets the relation field, which replaces <c>#organization-field</c>.
    /// </summary>
    required public OrganizationField Field { get; init; }

    /// <summary>
    /// Gets the picker, which replaces <c>#organization-picker</c>.
    /// </summary>
    required public OrganizationPicker Picker { get; init; }
}
