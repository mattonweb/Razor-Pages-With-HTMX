namespace HTMX.Adapter.Customers.Contracts;

/// <summary>
/// One organization as its own page shows it: every value it holds, how many contacts it has, and who created, last
/// changed and archived it.
/// </summary>
public sealed record OrganizationSingleResponse
{
    /// <summary>
    /// Gets the organization's identifier.
    /// </summary>
    required public OrganizationId Id { get; init; }

    /// <summary>
    /// Gets the organization's name.
    /// </summary>
    required public OrganizationName Name { get; init; }

    /// <summary>
    /// Gets the industry the organization works in.
    /// </summary>
    required public Industry Industry { get; init; }

    /// <summary>
    /// Gets the organization's website, or <see langword="null"/> when it has none on record.
    /// </summary>
    public WebAddress? Website { get; init; }

    /// <summary>
    /// Gets how many contacts belong to the organization, archived ones included.
    /// </summary>
    required public RecordCount Contacts { get; init; }

    /// <summary>
    /// Gets the version the organization is at now: 1 when it was created, and one more for each change since.
    /// </summary>
    required public VersionNumber Version { get; init; }

    /// <summary>
    /// Gets when the organization was created, and by whom.
    /// </summary>
    required public RecordChange Created { get; init; }

    /// <summary>
    /// Gets when the organization was last changed, and by whom.
    /// </summary>
    required public RecordChange LastModified { get; init; }

    /// <summary>
    /// Gets when the organization was archived, and by whom, or <see langword="null"/> while it is active.
    /// </summary>
    public RecordChange? Archived { get; init; }
}
