namespace HTMX.Application.Web.Pages.Components;

using System.Collections.Generic;

/// <summary>
/// The products the Order lines page offers, invented for the demonstration.
/// </summary>
public static class OrderProducts
{
    /// <summary>
    /// Gets the products' names, in the order the select lists them.
    /// </summary>
    public static IReadOnlyList<string> Names { get; } =
    [
        "Placeholder pens",
        "Sample paper",
        "Mock mugs",
        "Dummy desk lamps",
    ];
}
