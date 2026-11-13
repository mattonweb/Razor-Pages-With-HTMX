namespace HTMX.Application.Web.Pages.Contacts;

using System.Collections.Generic;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// What the <c>_ContactFields</c> partial shows: the form's values, the values another save left after it overtook
/// this one, and the organization field in one of its two forms.
/// </summary>
/// <remarks>
/// The organization field starts as a select of every active organization, which works without HTMX; HTMX swaps it
/// for the relation field and its picker as the page loads. An answer to HTMX draws the relation field straight away,
/// since HTMX is known to be there.
/// </remarks>
public sealed record ContactFields
{
    /// <summary>
    /// Gets the form's values. The partial names its fields <c>Input.Name</c> and so on, as the page binds them.
    /// </summary>
    required public ContactInput Input { get; init; }

    /// <summary>
    /// Gets the contact as another save left them, or <see langword="null"/> when no save overtook this one.
    /// </summary>
    public ContactSingleResponse? Saved { get; init; }

    /// <summary>
    /// Gets the relation field to draw, or <see langword="null"/> to draw the select that HTMX enhances.
    /// </summary>
    public OrganizationField? Relation { get; init; }

    /// <summary>
    /// Gets the select's options: every active organization, by name. Empty when the relation field is drawn instead.
    /// </summary>
    required public IReadOnlyList<SelectListItem> OrganizationOptions { get; init; }

    /// <summary>
    /// Gets the address HTMX loads the relation field and its picker from, in place of the select.
    /// </summary>
    required public string EnhanceAction { get; init; }

    /// <summary>
    /// Gets the telephone number field: its rows, and where Add and Remove post the form.
    /// </summary>
    required public PhoneNumberField PhoneNumbers { get; init; }
}
