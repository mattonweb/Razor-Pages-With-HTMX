namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// Where a page of the organizations list lies: just after, or just before, one organization.
/// </summary>
/// <remarks>
/// This is key-based paging, also called keyset or seek paging. The next page asks for the organizations that sort
/// after the last one shown, which the database finds by seeking in an index, rather than for a page number, which an
/// offset turns into reading and throwing away every row before the page. The cost of a page then stays the same
/// however deep in the list it is.
/// </remarks>
public readonly record struct OrganizationCursor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationCursor"/> class.
    /// </summary>
    /// <param name="id">The organization the page lies next to.</param>
    /// <param name="position">Which side of that organization the page lies on.</param>
    public OrganizationCursor(OrganizationId id, CursorPosition position)
    {
        if (!Enum.IsDefined(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "The position must be After or Before.");
        }

        this.Id = id;
        this.Position = position;
    }

    /// <summary>
    /// Gets the organization the page lies next to.
    /// </summary>
    public OrganizationId Id { get; }

    /// <summary>
    /// Gets which side of that organization the page lies on.
    /// </summary>
    public CursorPosition Position { get; }
}
