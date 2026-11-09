namespace HTMX.Test.Unit.Application.Web.Pages;

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Stands in for the cookie that carries TempData between requests in page tests: every request starts with nothing
/// waiting, and what a page leaves is read from its TempData directly rather than from a cookie.
/// </summary>
internal sealed class StubTempDataProvider : ITempDataProvider
{
    /// <inheritdoc/>
    public IDictionary<string, object> LoadTempData(HttpContext context)
    {
        return new Dictionary<string, object>();
    }

    /// <inheritdoc/>
    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
        // Nothing to save: the test reads the page's TempData itself.
    }
}
