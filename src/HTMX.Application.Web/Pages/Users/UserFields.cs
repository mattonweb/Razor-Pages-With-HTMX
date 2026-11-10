namespace HTMX.Application.Web.Pages.Users;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What the <c>_UserFields</c> partial shows: the form's values and, after another save overtook this one, the values
/// that save left, to show beside the ones being edited.
/// </summary>
public sealed record UserFields
{
    /// <summary>
    /// Gets the form's values. The partial names its fields <c>Input.Name</c> and so on, as the page binds them.
    /// </summary>
    required public UserInput Input { get; init; }

    /// <summary>
    /// Gets the user as another save left them, or <see langword="null"/> when no save overtook this one.
    /// </summary>
    public UserSingleResponse? Saved { get; init; }
}
