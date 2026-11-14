namespace HTMX.Application.Web.Authentication;

using System;
using System.Threading.Tasks;
using HTMX.Application.Web.Pages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

/// <summary>
/// What a cookie sign-in answers when a request needs a signed-in user and has none, as when the sign-in has expired:
/// for the browser on its own, a redirect to the sign-in page; for HTMX, a request to load that page with a reload.
/// </summary>
/// <remarks>
/// A redirect is wrong for HTMX. <c>fetch</c> follows it itself, so HTMX gets the sign-in page as the answer and swaps
/// it into the part of the page that sent the request: a whole page inside a card. And a sign-in page on another site
/// cannot be fetched at all. So HTMX gets 204 and <c>HX-Redirect</c>, which it follows with a full page load.
/// </remarks>
public static class HtmxCookieEvents
{
    /// <summary>
    /// Sends the request to the sign-in page. For HTMX the return address is the page the user was on when HTMX asked
    /// for part of it, which HTMX names in <c>HX-Current-URL</c>, rather than the address of the part; for a whole
    /// page, as a boosted link or Back asks for, it is that page.
    /// </summary>
    /// <param name="context">The cookie sign-in's redirect, with its options and the address it would send.</param>
    /// <returns>A task that is already complete.</returns>
    public static Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;
        if (!HtmxHeaders.IsHtmxRequest(request))
        {
            response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }

        string requested = request.PathBase + request.Path + request.QueryString;
        string? current = HtmxHeaders.IsPartialRequest(request) ? HtmxHeaders.CurrentPage(request) : null;
        CookieAuthenticationOptions options = context.Options;
        QueryString returnTo = QueryString.Create(options.ReturnUrlParameter, current ?? requested);
        response.StatusCode = StatusCodes.Status204NoContent;
        HtmxHeaders.LoadPageWithReload(response, request.PathBase + options.LoginPath + returnTo);
        return Task.CompletedTask;
    }
}
