namespace HTMX.Application.Web.Pages.Components.Icon;

using System.Collections.Generic;

/// <summary>
/// The icons the Icon page shows: every symbol in <c>wwwroot/icons.svg</c> but the logo, which has colors of its own,
/// grouped and ordered as the sprite groups them. An icon drawn into the sprite is added here too; an integration test
/// fails until the page shows every symbol the sprite holds.
/// </summary>
public static class IconGroups
{
    /// <summary>
    /// Gets the groups, in the sprite's order.
    /// </summary>
    public static IReadOnlyList<IconGroup> All { get; } =
    [
        new IconGroup
        {
            Title = "Navigation and layout",
            Names = ["menu", "home", "users", "user", "building", "palette", "list", "external"],
        },
        new IconGroup
        {
            Title = "Direction",
            Names = ["chevron-left", "chevron-right", "chevron-down", "arrow-up", "arrow-down", "sort"],
        },
        new IconGroup
        {
            Title = "Actions",
            Names =
            [
                "search", "filter", "plus", "more", "eye", "edit", "archive", "restore", "trash", "copy", "history",
                "x", "check",
            ],
        },
        new IconGroup
        {
            Title = "Status",
            Names = ["info", "check-circle", "alert", "x-circle", "inbox"],
        },
        new IconGroup
        {
            Title = "Theme",
            Names = ["sun", "moon"],
        },
    ];
}
