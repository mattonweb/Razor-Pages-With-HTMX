namespace HTMX.Application.Web.Pages.Contacts;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What the <c>_OrganizationPicker</c> dialog and the <c>_OrganizationChoices</c> list in it show: the first
/// organizations that match the filter, which one is chosen, and where the filter and the choice are sent.
/// </summary>
public sealed record OrganizationPicker
{
    /// <summary>
    /// Gets the active organizations that match the filter, the first few of them, and how many match in all.
    /// </summary>
    required public OrganizationCollectionResponse Choices { get; init; }

    /// <summary>
    /// Gets the organization chosen now, whose radio is checked when it is listed, or <see langword="null"/> for none.
    /// </summary>
    public OrganizationId? Chosen { get; init; }

    /// <summary>
    /// Gets the address the filter is sent to, which answers with the list alone.
    /// </summary>
    required public string ChoicesAction { get; init; }

    /// <summary>
    /// Gets the address a choice is sent to, which answers with the field and the picker drawn again.
    /// </summary>
    required public string FieldAction { get; init; }
}
