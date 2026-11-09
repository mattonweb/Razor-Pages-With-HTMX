namespace HTMX.Application.Web.Pages;

using System.Collections.Generic;

/// <summary>
/// What the <c>_Diff</c> partial shows: the fields that changed, under headings that say which values are which.
/// </summary>
public sealed record Diff
{
    /// <summary>
    /// Gets the table's caption, which screen readers announce and the page hides.
    /// </summary>
    required public string Caption { get; init; }

    /// <summary>
    /// Gets the heading over the values before, such as Before or Now (v3).
    /// </summary>
    required public string BeforeHeading { get; init; }

    /// <summary>
    /// Gets the heading over the values after, such as After or After the roll-back.
    /// </summary>
    required public string AfterHeading { get; init; }

    /// <summary>
    /// Gets the fields that changed, and only those.
    /// </summary>
    required public IReadOnlyList<FieldChange> Changes { get; init; }
}
