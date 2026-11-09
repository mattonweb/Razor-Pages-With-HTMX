namespace HTMX.Adapter.Customers.Contracts;

using System;

/// <summary>
/// Where a page of an organization's contacts lies: just after, or just before, one contact.
/// </summary>
/// <remarks>
/// Key-based paging, as for <see cref="OrganizationCursor"/>: the next page asks for the contacts whose names sort
/// after the last one shown, which the database finds by seeking in an index, rather than for a page number.
/// </remarks>
public readonly record struct ContactCursor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContactCursor"/> class.
    /// </summary>
    /// <param name="id">The contact the page lies next to.</param>
    /// <param name="position">Which side of that contact the page lies on.</param>
    public ContactCursor(ContactId id, CursorPosition position)
    {
        if (!Enum.IsDefined(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "The position must be After or Before.");
        }

        this.Id = id;
        this.Position = position;
    }

    /// <summary>
    /// Gets the contact the page lies next to.
    /// </summary>
    public ContactId Id { get; }

    /// <summary>
    /// Gets which side of that contact the page lies on.
    /// </summary>
    public CursorPosition Position { get; }
}
