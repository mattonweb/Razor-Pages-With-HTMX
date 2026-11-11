namespace HTMX.Application.Web.Pages;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

/// <summary>
/// The HTMX 4 headers the pages read and write: whether HTMX sent a request, whether it will swap part of the page,
/// and how to send the browser to another page after a save.
/// </summary>
public static class HtmxHeaders
{
    // HTMX sends this, set to "true", on every request it makes; a browser loading a page on its own does not.
    private const string Request = "HX-Request";

    // HTMX 4 says on every request whether it will swap part of the page ("partial") or the whole body ("full"), as
    // when the back button restores a page, or a page's part that hx-select picks from it.
    private const string RequestType = "HX-Request-Type";
    private const string PartialRequestType = "partial";

    // Asks HTMX to load another page as a boosted link would: a GET whose answer replaces the body, with its URL
    // pushed into the history.
    private const string Location = "HX-Location";

    /// <summary>
    /// Says whether HTMX sent a request, for part of a page or for the whole of one, rather than the browser on its
    /// own.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns><see langword="true"/> when HTMX sent it.</returns>
    public static bool IsHtmxRequest(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        IHeaderDictionary headers = request.Headers;
        return headers[Request] == "true";
    }

    /// <summary>
    /// Says whether a request came from HTMX to swap part of a page, rather than to load a whole one.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns><see langword="true"/> when HTMX will swap the response into part of the page.</returns>
    public static bool IsPartialRequest(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        IHeaderDictionary headers = request.Headers;
        return headers[RequestType] == PartialRequestType;
    }

    /// <summary>
    /// Says that the response depends on whether HTMX asked for part of the page, so that a cache keeps the two
    /// answers to one URL apart. The header is added to any Vary already set, so what another part of the application
    /// varies by is kept.
    /// </summary>
    /// <param name="response">The response.</param>
    public static void VaryByRequestType(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        IHeaderDictionary headers = response.Headers;
        headers.Append(HeaderNames.Vary, RequestType);
    }

    /// <summary>
    /// Asks HTMX to load another page in place of this one, with its URL pushed into the history. A save that HTMX
    /// sent answers this way rather than with a redirect: <c>fetch</c> would follow the redirect itself and HTMX would
    /// swap the whole of the next page into the part of this one that sent the save.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <param name="url">The page to load.</param>
    public static void LoadPage(HttpResponse response, string url)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        IHeaderDictionary headers = response.Headers;
        headers[Location] = url;
    }
}
