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
    /// The values sent are the values the record already holds, so nothing was saved and no version was made.
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
    /// Another record already has the name the changes give, however it is capitalized.
    /// </summary>
    NameTaken,
}
