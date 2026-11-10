namespace HTMX.Adapter.Customers.Providers;

using System;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The LIKE patterns the providers filter with: the filter's text found anywhere in a value, with any % or _ in it
/// taken literally.
/// </summary>
internal static class LikePattern
{
    /// <summary>
    /// The character that makes the next % or _ in a pattern literal. A query gives it to LIKE beside the pattern.
    /// </summary>
    internal const string Escape = "\\";

    /// <summary>
    /// Makes a LIKE pattern that finds the text anywhere in a value, with any % or _ in it taken literally.
    /// </summary>
    /// <param name="filter">The text to find.</param>
    /// <returns>The pattern.</returns>
    internal static string Contains(FilterText filter)
    {
        string escaped = filter.Value.Replace(Escape, Escape + Escape, StringComparison.Ordinal);
        escaped = escaped.Replace("%", Escape + "%", StringComparison.Ordinal);
        escaped = escaped.Replace("_", Escape + "_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }
}
