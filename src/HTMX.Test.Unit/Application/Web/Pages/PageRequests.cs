namespace HTMX.Test.Unit.Application.Web.Pages;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Builds the context a page model has for one request, as the framework would: Demo User signed in, the headers HTMX
/// 4 sends for the kind of request, and the page named in the route, from which relative page names are resolved.
/// </summary>
internal static class PageRequests
{
    /// <summary>
    /// Creates the context of one request to a page.
    /// </summary>
    /// <param name="kind">What sent the request.</param>
    /// <param name="page">The page the request is for, such as <c>/Organizations/Edit</c>.</param>
    /// <returns>The context, with view data the page's partials can render from.</returns>
    internal static PageContext Create(HtmxRequest kind, string page)
    {
        Claim[] claims = [new Claim(ClaimTypes.NameIdentifier, "10001")];
        DefaultHttpContext http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")),
        };
        IHeaderDictionary headers = http.Request.Headers;
        if (kind == HtmxRequest.Partial || kind == HtmxRequest.Boosted)
        {
            headers["HX-Request"] = "true";
        }

        if (kind != HtmxRequest.None)
        {
            headers["HX-Request-Type"] = kind == HtmxRequest.Partial ? "partial" : "full";
        }

        if (kind == HtmxRequest.HistoryRestore)
        {
            headers["HX-History-Restore-Request"] = "true";
        }

        if (kind == HtmxRequest.Boosted)
        {
            headers["HX-Boosted"] = "true";
        }

        RouteData route = new RouteData();
        route.Values["page"] = page;
        EmptyModelMetadataProvider metadata = new EmptyModelMetadataProvider();
        ActionContext action = new ActionContext(http, route, new CompiledPageActionDescriptor());
        return new PageContext(action)
        {
            ViewData = new ViewDataDictionary(metadata, new ModelStateDictionary()),
        };
    }
}
