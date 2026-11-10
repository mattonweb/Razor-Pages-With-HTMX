namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// Where a page of the users list lies: just after, or just before, one user.
/// </summary>
/// <remarks>
/// Key-based paging, as for <see cref="OrganizationCursor"/>: the next page asks for the users that sort after the last
/// one shown, which the database finds by seeking in an index, never for a page number.
/// </remarks>
public readonly record struct UserCursor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserCursor"/> class.
    /// </summary>
    /// <param name="id">The user the page lies next to.</param>
    /// <param name="position">Which side of that user the page lies on.</param>
    public UserCursor(UserId id, CursorPosition position)
    {
        if (!Enum.IsDefined(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "The position must be After or Before.");
        }

        this.Id = id;
        this.Position = position;
    }

    /// <summary>
    /// Gets the user the page lies next to.
    /// </summary>
    public UserId Id { get; }

    /// <summary>
    /// Gets which side of that user the page lies on.
    /// </summary>
    public CursorPosition Position { get; }
}
