namespace HTMX.Application.Web.Pages.Organizations;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The Details tab of an organization's page, at /organizations/{id}: its name, status and version, who changed it
/// last, and every value it holds. An identifier that names no organization gets the page saying so, with the status
/// 404 Not Found. Archive and Restore, and the toast they leave, are <see cref="OrganizationPageModel"/>'s.
/// </summary>
public sealed class DetailsModel : OrganizationPageModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="organizations">Reads, archives and restores the organization.</param>
    public DetailsModel(IOrganizationService organizations)
        : base(organizations)
    {
    }

    /// <inheritdoc/>
    public override string PageName => "./Details";

    /// <inheritdoc/>
    public override string ContentPartial => "_OrganizationDetails";
}
