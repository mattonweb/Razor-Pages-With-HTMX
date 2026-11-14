namespace HTMX.Application.Web.Pages.Components;

/// <summary>
/// One article on the Not allowed page's desk: its number, its title and whether it is published.
/// </summary>
public sealed record Article
{
    /// <summary>
    /// Gets the article's number, from 10001, which its row's form posts.
    /// </summary>
    required public long Number { get; init; }

    /// <summary>
    /// Gets the article's title.
    /// </summary>
    required public string Title { get; init; }

    /// <summary>
    /// Gets a value indicating whether the article is published, rather than a draft.
    /// </summary>
    required public bool Published { get; init; }
}
