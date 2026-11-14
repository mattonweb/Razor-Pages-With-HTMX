namespace HTMX.Application.Web.Pages.Components;

using System;

/// <summary>
/// The Not allowed page's desk: a few invented articles, each published or a draft, one desk for everyone who opens
/// the page.
/// </summary>
public sealed class ArticleDesk
{
    // The articles, in the order the list shows them. Every read and write locks it.
    private readonly Article[] _articles =
    [
        new Article { Number = 10001, Title = "Placeholder opening hours", Published = true },
        new Article { Number = 10002, Title = "Sample spring newsletter", Published = false },
        new Article { Number = 10003, Title = "Mock price list", Published = false },
    ];

    /// <summary>
    /// Reads every article, in the order the list shows them.
    /// </summary>
    /// <returns>The articles.</returns>
    public Article[] All()
    {
        lock (this._articles)
        {
            return [.. this._articles];
        }
    }

    /// <summary>
    /// Publishes an article.
    /// </summary>
    /// <param name="number">The article's number.</param>
    /// <returns>The article as it is now, or <see langword="null"/> when the desk has none by that number.</returns>
    public Article? Publish(long number)
    {
        return this.Mark(number, published: true);
    }

    /// <summary>
    /// Makes an article a draft again.
    /// </summary>
    /// <param name="number">The article's number.</param>
    /// <returns>The article as it is now, or <see langword="null"/> when the desk has none by that number.</returns>
    public Article? Unpublish(long number)
    {
        return this.Mark(number, published: false);
    }

    /// <summary>
    /// Marks an article published or a draft.
    /// </summary>
    /// <param name="article">The article.</param>
    /// <param name="published">Whether it is published.</param>
    /// <returns>The article, so marked.</returns>
    private static Article Marked(Article article, bool published)
    {
        return article with { Published = published };
    }

    /// <summary>
    /// Marks the article with a number published or a draft.
    /// </summary>
    /// <param name="number">The article's number.</param>
    /// <param name="published">Whether it is published.</param>
    /// <returns>The article as it is now, or <see langword="null"/> when the desk has none by that number.</returns>
    private Article? Mark(long number, bool published)
    {
        lock (this._articles)
        {
            int position = Array.FindIndex(this._articles, article => article.Number == number);
            if (position < 0)
            {
                return null;
            }

            this._articles[position] = Marked(this._articles[position], published);
            return this._articles[position];
        }
    }
}
