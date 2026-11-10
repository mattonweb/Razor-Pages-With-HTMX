namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What became of a request to change a record. Each is an answer the caller expects and shows; none is an error.
/// </summary>
public enum UpdateOutcome
{
    /// <summary>
    /// The changes were saved as the record's next version.
    /// </summary>
    Saved,

    /// <summary>
    /// The record already holds the values sent, or is already archived or active as asked, so nothing was saved and
    /// no version was made.
    /// </summary>
    Unchanged,

    /// <summary>
    /// No record has the identifier: it never existed, or it has been deleted.
    /// </summary>
    NotFound,

    /// <summary>
    /// The record is archived, and an archived record is closed to edits until it is restored.
    /// </summary>
    Archived,

    /// <summary>
    /// Another save reached the record after the version the changes started from, perhaps the same user's from
    /// another tab, so the changes were not saved over it.
    /// </summary>
    Overtaken,

    /// <summary>
    /// Another record of the same kind already has the name the changes give, however it is capitalized: another
    /// organization the organization's name, or another user the user's username.
    /// </summary>
    NameTaken,

    /// <summary>
    /// The changes would put a contact in an organization that is archived, and so takes no one in, or that no
    /// longer exists.
    /// </summary>
    OrganizationUnavailable,

    /// <summary>
    /// Another user already has the email address the changes give, however it is capitalized.
    /// </summary>
    EmailTaken,

    /// <summary>
    /// The request would archive the user who sent it, who would then be shut out of the application. Another user
    /// must archive them.
    /// </summary>
    SelfArchive,
}
