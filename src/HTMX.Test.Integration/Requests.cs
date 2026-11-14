namespace HTMX.Test.Integration;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

/// <summary>
/// Sends requests as the browser on its own or HTMX would, and reads each answer in full.
/// </summary>
internal static class Requests
{
    /// <summary>
    /// Sends a GET, with the headers HTMX sends for the kind of request.
    /// </summary>
    /// <param name="browser">The client, which keeps cookies as a browser does.</param>
    /// <param name="address">The page's address, such as <c>/organizations</c>.</param>
    /// <param name="kind">What sends the request.</param>
    /// <returns>The answer, read in full.</returns>
    internal static async Task<Answer> GetAsync(HttpClient browser, string address, HtmxRequest kind)
    {
        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(address, UriKind.Relative)))
        {
            AddHeaders(request, kind);
            return await SendAsync(browser, request).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sends a GET, with the headers HTMX sends for the kind of request and more of HTMX's own, such as
    /// <c>HX-Current-URL</c>, which names the page the request comes from.
    /// </summary>
    /// <param name="browser">The client, which keeps cookies as a browser does.</param>
    /// <param name="address">The page's address.</param>
    /// <param name="kind">What sends the request.</param>
    /// <param name="headers">The other headers.</param>
    /// <returns>The answer, read in full.</returns>
    internal static async Task<Answer> GetAsync(
        HttpClient browser,
        string address,
        HtmxRequest kind,
        IReadOnlyDictionary<string, string> headers)
    {
        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(address, UriKind.Relative)))
        {
            AddHeaders(request, kind);
            foreach (KeyValuePair<string, string> header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            return await SendAsync(browser, request).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Posts a form's fields, URL-encoded as a browser and HTMX send them, with the headers HTMX sends for the kind of
    /// request.
    /// </summary>
    /// <param name="browser">The client, which keeps cookies as a browser does.</param>
    /// <param name="address">The form's action, such as <c>/organizations/create</c>.</param>
    /// <param name="kind">What sends the request.</param>
    /// <param name="fields">The form's fields, the antiforgery token among them when the test sends one.</param>
    /// <returns>The answer, read in full.</returns>
    internal static async Task<Answer> PostAsync(
        HttpClient browser,
        string address,
        HtmxRequest kind,
        IEnumerable<KeyValuePair<string, string>> fields)
    {
        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(address, UriKind.Relative)))
        {
            request.Content = new FormUrlEncodedContent(fields);
            AddHeaders(request, kind);
            return await SendAsync(browser, request).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Posts from an element with no form around it, as HTMX does: no fields, and the headers the element names in
    /// <c>hx-headers</c>, such as the antiforgery token's, with the headers HTMX sends for the kind of request.
    /// </summary>
    /// <param name="browser">The client, which keeps cookies as a browser does.</param>
    /// <param name="address">The element's <c>hx-post</c>.</param>
    /// <param name="kind">What sends the request.</param>
    /// <param name="headers">The element's own headers.</param>
    /// <returns>The answer, read in full.</returns>
    internal static async Task<Answer> PostWithoutFormAsync(
        HttpClient browser,
        string address,
        HtmxRequest kind,
        IReadOnlyDictionary<string, string> headers)
    {
        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(address, UriKind.Relative)))
        {
            request.Content = new FormUrlEncodedContent([]);
            AddHeaders(request, kind);
            foreach (KeyValuePair<string, string> header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            return await SendAsync(browser, request).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Posts a form that sends a file, as multipart form data, as a browser and HTMX send a form that says
    /// <c>enctype="multipart/form-data"</c>, with the headers HTMX sends for the kind of request.
    /// </summary>
    /// <param name="browser">The client, which keeps cookies as a browser does.</param>
    /// <param name="address">The form's action.</param>
    /// <param name="kind">What sends the request.</param>
    /// <param name="fields">The form's other fields, the antiforgery token among them.</param>
    /// <param name="file">The file's field, its file name and its bytes.</param>
    /// <returns>The answer, read in full.</returns>
    internal static async Task<Answer> PostFileAsync(
        HttpClient browser,
        string address,
        HtmxRequest kind,
        IEnumerable<KeyValuePair<string, string>> fields,
        FilePart file)
    {
        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(address, UriKind.Relative)))
        {
            MultipartFormDataContent content = new MultipartFormDataContent();
            foreach (KeyValuePair<string, string> field in fields)
            {
                content.Add(new StringContent(field.Value), field.Key);
            }

            content.Add(new ByteArrayContent(file.Content), file.Field, file.FileName);
            request.Content = content;
            AddHeaders(request, kind);
            return await SendAsync(browser, request).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Loads a page as the browser does and reads the antiforgery token its forms post. Loading it also sets the
    /// antiforgery cookie, which the client sends back with the form.
    /// </summary>
    /// <param name="browser">The client, which keeps cookies as a browser does.</param>
    /// <param name="address">The page's address.</param>
    /// <returns>The token.</returns>
    internal static async Task<string> ReadAntiforgeryTokenAsync(HttpClient browser, string address)
    {
        Answer page = await GetAsync(browser, address, HtmxRequest.None).ConfigureAwait(false);
        return Html.AntiforgeryToken(page.Body);
    }

    /// <summary>
    /// Adds the headers HTMX 4.0.1 sends for a kind of request. Its request for Back and Forward has no HX-Request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="kind">What sends it.</param>
    private static void AddHeaders(HttpRequestMessage request, HtmxRequest kind)
    {
        HttpRequestHeaders headers = request.Headers;
        if (kind == HtmxRequest.Partial || kind == HtmxRequest.Boosted)
        {
            headers.Add("HX-Request", "true");
        }

        if (kind != HtmxRequest.None)
        {
            headers.Add("HX-Request-Type", kind == HtmxRequest.Partial ? "partial" : "full");
        }

        if (kind == HtmxRequest.Boosted)
        {
            headers.Add("HX-Boosted", "true");
        }

        if (kind == HtmxRequest.HistoryRestore)
        {
            headers.Add("HX-History-Restore-Request", "true");
        }
    }

    /// <summary>
    /// Sends a request and reads the whole answer: status, headers and body.
    /// </summary>
    /// <param name="browser">The client.</param>
    /// <param name="request">The request.</param>
    /// <returns>The answer.</returns>
    private static async Task<Answer> SendAsync(HttpClient browser, HttpRequestMessage request)
    {
        using (HttpResponseMessage response = await browser.SendAsync(request).ConfigureAwait(false))
        {
            HttpContent content = response.Content;
            string body = await content.ReadAsStringAsync().ConfigureAwait(false);
            Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AddAll(headers, response.Headers);
            AddAll(headers, content.Headers);
            return new Answer { Status = response.StatusCode, Headers = headers, Body = body };
        }
    }

    /// <summary>
    /// Copies headers into a dictionary, each with its values joined by commas.
    /// </summary>
    /// <param name="into">The dictionary.</param>
    /// <param name="headers">The headers.</param>
    private static void AddAll(Dictionary<string, string> into, HttpHeaders headers)
    {
        foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
        {
            into[header.Key] = string.Join(", ", header.Value);
        }
    }
}
