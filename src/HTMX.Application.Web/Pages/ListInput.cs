namespace HTMX.Application.Web.Pages;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// A list page's query string, as the browser sent it. Every value is optional, and is kept as sent until the page
/// turns it into a request; <see cref="ListQuery"/> says how each is read.
/// </summary>
public sealed class ListInput
{
    /// <summary>
    /// Gets or sets the text the list's records must contain.
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Gets or sets which records to show: active, archived or all.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets what to sort by: the name of one of the list's columns.
    /// </summary>
    public string? Sort { get; set; }

    /// <summary>
    /// Gets or sets which way to sort: ascending or descending. The query string calls it dir.
    /// </summary>
    [FromQuery(Name = ListQuery.DirectionKey)]
    public string? Direction { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the record the page starts after, for Next.
    /// </summary>
    public long? After { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the record the page ends before, for Previous.
    /// </summary>
    public long? Before { get; set; }

    /// <summary>
    /// Gets or sets how many records a page holds: one of <see cref="ListQuery.PageSizes"/>. The query string calls it
    /// size.
    /// </summary>
    [FromQuery(Name = ListQuery.PageSizeKey)]
    public int? PageSize { get; set; }
}
