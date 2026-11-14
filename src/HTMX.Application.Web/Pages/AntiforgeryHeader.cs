namespace HTMX.Application.Web.Pages;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

/// <summary>
/// The antiforgery token as a request header, for an element that sends a request no form could send.
/// </summary>
/// <remarks>
/// A form that posts carries the token in a hidden field, and HTMX sends it with the form's other fields. An element
/// with no form, such as one whose request a scroll sends, sends no fields, so the token goes in the header the
/// antiforgery check also reads, named by <see cref="AntiforgeryTokenSet.HeaderName"/>: <c>RequestVerificationToken</c>
/// unless the application changes it.
/// </remarks>
public static class AntiforgeryHeader
{
    /// <summary>
    /// Writes an <c>hx-headers</c> value that sends the request's antiforgery token in that header, such as
    /// <c>{"RequestVerificationToken":"CfDJ8…"}</c>. As a form's token does, it also sets the antiforgery cookie the
    /// token is checked against.
    /// </summary>
    /// <param name="antiforgery">The application's antiforgery service.</param>
    /// <param name="context">The request the page answers.</param>
    /// <returns>The value, as JSON, which HTMX reads as it is and which a Content Security Policy allows.</returns>
    public static string HxHeaders(IAntiforgery antiforgery, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(antiforgery);
        ArgumentNullException.ThrowIfNull(context);
        AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
        string header = tokens.HeaderName
            ?? throw new InvalidOperationException("The antiforgery check reads no header, so it has none to send.");
        Dictionary<string, string?> headers = new Dictionary<string, string?> { [header] = tokens.RequestToken };
        return JsonSerializer.Serialize(headers);
    }
}
