namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// What became of a request to create a record. Each is an answer the caller expects and shows; none is an error.
/// </summary>
public enum CreateOutcome
{
    /// <summary>
    /// The record was created, with its first version.
    /// </summary>
    Created,

    /// <summary>
    /// Another record already has the name given, however it is capitalized, so nothing was created.
    /// </summary>
    NameTaken,
}
