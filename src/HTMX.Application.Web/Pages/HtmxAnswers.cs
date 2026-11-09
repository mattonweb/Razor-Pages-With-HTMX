namespace HTMX.Application.Web.Pages;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The two answers a page handler gives by whether HTMX sent the request: the part of the page HTMX will swap or the
/// whole page, and, after a save, the next page by a redirect or by asking HTMX to load it.
/// </summary>
public static class HtmxAnswers
{
    /// <summary>
    /// Answers with a partial when HTMX will swap it into the page it already shows, and with the whole page otherwise.
    /// The partial's model is the page model, as the page itself renders it.
    /// </summary>
    /// <param name="page">The page answering.</param>
    /// <param name="partialName">The partial HTMX swaps.</param>
    /// <returns>The partial or the page.</returns>
    public static IActionResult PartOrPage(PageModel page, string partialName)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (HtmxHeaders.IsPartialRequest(page.Request))
        {
            return page.Partial(partialName, page);
        }

        return page.Page();
    }

    /// <summary>
    /// Goes to the next page after a save. Without HTMX that is a redirect, so that refreshing the next page does not
    /// post the form again. With HTMX it is 204 No Content and a request to load the page, because <c>fetch</c> would
    /// follow a redirect itself and HTMX would swap the whole of the next page into the form.
    /// </summary>
    /// <param name="page">The page answering.</param>
    /// <param name="pageName">The next page, such as <c>./Details</c>.</param>
    /// <param name="routeValues">The next page's route values, such as its record's identifier.</param>
    /// <returns>The redirect, or 204 with the request to load the page.</returns>
    public static IActionResult NextPage(PageModel page, string pageName, object routeValues)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (!HtmxHeaders.IsPartialRequest(page.Request))
        {
            return page.RedirectToPage(pageName, routeValues);
        }

        IUrlHelper url = page.Url;
        string address = url.Page(pageName, routeValues)
            ?? throw new InvalidOperationException($"The page {pageName} has no address.");
        HtmxHeaders.LoadPage(page.Response, address);
        return page.StatusCode(StatusCodes.Status204NoContent);
    }
}
