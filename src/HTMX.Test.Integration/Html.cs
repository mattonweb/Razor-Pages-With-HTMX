namespace HTMX.Test.Integration;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Reads what a test needs from the HTML an answer holds, without a browser.
/// </summary>
internal static class Html
{
    // The hidden field Razor writes in every form that posts, which holds the antiforgery token.
    private static readonly Regex TokenField = new Regex(
        "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    // A page's heading that takes the keyboard when the page arrives, as Razor writes a true autofocus.
    private static readonly Regex FocusedHeadingTag = new Regex(
        "<h1[^>]*\\bautofocus\\b",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// Says whether HTML is a whole page, in the application's layout, rather than a partial.
    /// </summary>
    /// <param name="html">The HTML.</param>
    /// <returns><see langword="true"/> for a whole page.</returns>
    internal static bool IsWholePage(string html)
    {
        return html.Contains("<html", StringComparison.Ordinal);
    }

    /// <summary>
    /// Says whether the page's heading takes the keyboard when the HTML arrives.
    /// </summary>
    /// <param name="html">The HTML.</param>
    /// <returns><see langword="true"/> when the heading says autofocus.</returns>
    internal static bool HeadingFocused(string html)
    {
        return FocusedHeadingTag.IsMatch(html);
    }

    /// <summary>
    /// Reads the antiforgery token from the first form on a page that posts.
    /// </summary>
    /// <param name="html">The page's HTML.</param>
    /// <returns>The token.</returns>
    internal static string AntiforgeryToken(string html)
    {
        Match match = TokenField.Match(html);
        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException("The page has no form that posts, so no antiforgery token.");
    }

    /// <summary>
    /// Builds a form's fields, the antiforgery token first.
    /// </summary>
    /// <param name="token">The antiforgery token.</param>
    /// <param name="fields">The form's other fields, by name; a name may come more than once, as rows that share a
    /// name post.</param>
    /// <returns>The fields, ready to post.</returns>
    internal static IEnumerable<KeyValuePair<string, string>> Form(
        string token,
        IEnumerable<KeyValuePair<string, string>> fields)
    {
        List<KeyValuePair<string, string>> form = [new KeyValuePair<string, string>("__RequestVerificationToken", token)];
        form.AddRange(fields);
        return form;
    }
}
