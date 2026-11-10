namespace HTMX.Application.Web.Pages.Users;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The Details tab of a user's page, at /users/{id}: their name, status and version, who changed them last, and every
/// value they hold. An identifier that names no user gets the page saying so, with the status 404 Not Found. Archive
/// and Restore, and the toast they leave, are <see cref="UserPageModel"/>'s.
/// </summary>
public sealed class DetailsModel : UserPageModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="users">Reads, archives and restores the user.</param>
    public DetailsModel(IUserService users)
        : base(users)
    {
    }

    /// <inheritdoc/>
    public override string PageName => "./Details";

    /// <inheritdoc/>
    public override string ContentPartial => "_UserDetails";
}
