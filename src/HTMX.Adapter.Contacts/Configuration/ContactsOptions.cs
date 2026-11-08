namespace HTMX.Adapter.Contacts.Configuration;

/// <summary>
/// What the host supplies to register the contacts adapter. The adapter reads no configuration of its own.
/// </summary>
public sealed record ContactsOptions
{
    /// <summary>
    /// Gets the name of the in-memory database the adapter creates when the application starts.
    /// </summary>
    required public DatabaseName DatabaseName { get; init; }
}
