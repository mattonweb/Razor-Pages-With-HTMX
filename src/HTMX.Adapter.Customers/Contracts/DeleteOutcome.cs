namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What became of a request to delete a record permanently. Each is an answer the caller expects and shows; none is an
/// error.
/// </summary>
public enum DeleteOutcome
{
    /// <summary>
    /// The record and its history were deleted. It cannot be undone.
    /// </summary>
    Deleted,

    /// <summary>
    /// No record has the identifier: it never existed, or it has been deleted already.
    /// </summary>
    NotFound,

    /// <summary>
    /// The record is active. Only an archived record can be deleted, so that nothing is deleted in one step.
    /// </summary>
    Active,

    /// <summary>
    /// Other records still need the record, such as a user whom other records' histories name, or an organization that
    /// contacts belong to, and would be left naming nothing, so it stays archived instead.
    /// </summary>
    InUse,

    /// <summary>
    /// The record has changed since the page asked to delete it, such as being restored and archived again, so it was
    /// not deleted.
    /// </summary>
    Overtaken,
}
