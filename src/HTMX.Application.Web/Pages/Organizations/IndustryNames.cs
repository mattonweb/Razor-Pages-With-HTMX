namespace HTMX.Application.Web.Pages.Organizations;

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The names the organization pages show for industries, such as Food and drink, taken from each value's
/// <see cref="DisplayAttribute"/>.
/// </summary>
public static class IndustryNames
{
    /// <summary>
    /// Gives an industry's name as the pages show it.
    /// </summary>
    /// <param name="industry">The industry.</param>
    /// <returns>The display name its <see cref="DisplayAttribute"/> gives, or the value's own name without
    /// one.</returns>
    public static string Of(Industry industry)
    {
        string name = industry.ToString();
        FieldInfo? field = typeof(Industry).GetField(name);
        DisplayAttribute? display = field?.GetCustomAttribute<DisplayAttribute>();
        return display?.GetName() ?? name;
    }
}
