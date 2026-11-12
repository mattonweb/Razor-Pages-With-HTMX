namespace HTMX.Test.Unit.Application.Web.Pages;

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Stands in for routing in page tests: a page's address comes back as the page's name and the identifier, when there
/// is one, then the handler and any other values, such as
/// <c>/Organizations/History/10003?handler=RollBack&amp;to=2&amp;version=3</c>, so a test sees which page, record,
/// handler and values a page model asked for without the application's routes.
/// </summary>
internal sealed class StubUrlHelper : IUrlHelper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StubUrlHelper"/> class.
    /// </summary>
    /// <param name="actionContext">The page's own context, whose route names the page that relative names start
    /// from.</param>
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
        object? id = values["id"];
        string address = id is null ? $"{values["page"]}" : $"{values["page"]}/{id}";
        string[] query =
        [
            .. values
                .Where(static value => value.Key is not "page" and not "id" && value.Value is not null)
                .OrderBy(static value => value.Key == "handler" ? string.Empty : value.Key, StringComparer.Ordinal)
                .Select(static value => $"{value.Key}={value.Value}"),
        ];
        return query.Length == 0 ? address : $"{address}?{string.Join('&', query)}";
    }
}
