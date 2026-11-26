namespace HTMX.Application.Web.Pages.Components.Icon;

using System.Collections.Generic;

/// <summary>
/// One group of the sprite's icons, as <c>wwwroot/icons.svg</c> groups them: its title and its icons' names.
/// </summary>
public sealed record IconGroup
{
    /// <summary>
    /// Gets the group's title, such as Actions.
    /// </summary>
    required public string Title { get; init; }

    /// <summary>
    /// Gets the names of the group's icons, in the sprite's order, each the id of one of its symbols.
    /// </summary>
    required public IReadOnlyList<string> Names { get; init; }
}
