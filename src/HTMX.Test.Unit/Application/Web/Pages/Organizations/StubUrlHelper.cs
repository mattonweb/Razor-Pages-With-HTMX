namespace HTMX.Test.Unit.Application.Web.Pages.Organizations;

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Stands in for routing in page tests: a page's address comes back as the page's name and the identifier, and the
/// handler if one is named, such as <c>/Organizations/Details/10003?handler=Restore</c>, so a test sees which page,
/// record and handler a page model asked for without the application's routes.
/// </summary>
internal sealed class StubUrlHelper : IUrlHelper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StubUrlHelper"/> class.
    /// </summary>
    /// <param name="actionContext">The page's own context, whose route names the page that relative names start from.</param>
    public StubUrlHelper(ActionContext actionContext)
    {
        this.ActionContext = actionContext;
    }

    /// <inheritdoc/>
    public ActionContext ActionContext { get; }

    /// <inheritdoc/>
    public string? Action(UrlActionContext actionContext)
    {
        throw new NotSupportedException("The pages link to pages, not to controller actions.");
    }

    /// <inheritdoc/>
    public string? Content(string? contentPath)
    {
        return contentPath;
    }

    /// <inheritdoc/>
    public bool IsLocalUrl(string? url)
    {
        return url is not null && url.StartsWith('/');
    }

    /// <inheritdoc/>
    public string? Link(string? routeName, object? values)
    {
        throw new NotSupportedException("The pages write addresses within the application.");
    }

    /// <inheritdoc/>
    public string? RouteUrl(UrlRouteContext routeContext)
    {
        ArgumentNullException.ThrowIfNull(routeContext);
        RouteValueDictionary values = new RouteValueDictionary(routeContext.Values);
        string address = $"{values["page"]}/{values["id"]}";
        return values.TryGetValue("handler", out object? handler) && handler is not null
            ? $"{address}?handler={handler}"
            : address;
    }
}
