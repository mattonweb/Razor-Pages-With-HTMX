namespace HTMX.Application.Web.Pages.Contacts;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The Details tab of a contact's page, at /contacts/{id}: their name, status and version, who changed them last,
/// the organization they belong to, and every value they hold. An identifier that names no contact gets the page
/// saying so, with the status 404 Not Found. Archive and Restore, and the toast they leave, are
/// <see cref="ContactPageModel"/>'s.
/// </summary>
public sealed class DetailsModel : ContactPageModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="contacts">Reads, archives and restores the contact.</param>
    public DetailsModel(IContactService contacts)
        : base(contacts)
    {
    }

    /// <inheritdoc/>
    public override string PageName => "./Details";

    /// <inheritdoc/>
    public override string ContentPartial => "_ContactDetails";
}
