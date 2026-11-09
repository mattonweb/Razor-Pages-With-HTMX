namespace HTMX.Application.Web.Pages.Organizations;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What the <c>_OrganizationFields</c> partial shows: the form's values and, after another save overtook this one, the
/// values that save left, to show beside the ones being edited.
/// </summary>
public sealed record OrganizationFields
{
    /// <summary>
    /// Gets the form's values. The partial names its fields <c>Input.Name</c> and so on, as the pages bind them.
    /// </summary>
    required public OrganizationInput Input { get; init; }

    /// <summary>
    /// Gets the organization as another save left it, or <see langword="null"/> when no save overtook this one.
    /// </summary>
    public OrganizationSingleResponse? Saved { get; init; }
}
