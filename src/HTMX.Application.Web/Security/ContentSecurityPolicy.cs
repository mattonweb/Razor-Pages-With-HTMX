namespace HTMX.Application.Web.Security;

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

/// <summary>
/// The Content Security Policy every response carries, which tells the browser what a page may load and run: scripts,
/// stylesheets and images from the application's own files, requests and form posts to the application, and nothing
/// else. Razor encodes what a page writes, so a user's text never becomes markup; if markup got in some other way, the
/// policy stops it running a script or sending what the page holds to another site.
/// </summary>
/// <remarks>
/// HTMX 4 needs nothing relaxed, because no page uses what HTMX compiles from a string: <c>hx-on</c>, a trigger filter
/// such as <c>click[shiftKey]</c>, or a <c>js:</c> value. Those need <c>'unsafe-eval'</c>, which would let a script
/// that got in compile more. No page holds an inline script or a <c>style</c> attribute either, which would need
/// <c>'unsafe-inline'</c>. The header is set before the rest of the pipeline runs, so it goes with every answer, the
/// production error page's too; the developer exception page clears it, and so keeps its own inline script and styles.
/// </remarks>
public static class ContentSecurityPolicy
{
    // Nothing loads unless a directive below allows it: no plugins, frames, fonts, media or workers.
    private const string Fallback = "default-src 'none'";

    // Scripts only from the application's own files: no inline script, no onclick attribute, and nothing compiled from
    // a string. A script inside an answer that HTMX swaps in is inline, so it never runs.
    private const string Scripts = "script-src 'self'";

    // Stylesheets only from the application's own files: no <style> element and no style attribute.
    private const string Styles = "style-src 'self'";

    // Images, the icon sprite among them, only from the application's own files.
    private const string Images = "img-src 'self'";

    // HTMX's requests only to the application.
    private const string Connections = "connect-src 'self'";

    // In development, the hot reload script that the SDK or the IDE adds to each page also talks to it, over a
    // WebSocket on another port of this machine.
    private const string DevelopmentConnections = "connect-src 'self' ws://localhost:* wss://localhost:*";

    // Forms post only to the application. This directive and the two below do not fall back to default-src.
    private const string Forms = "form-action 'self'";

    // No other site may show these pages in a frame, which is how clickjacking works.
    private const string Framing = "frame-ancestors 'none'";

    // No <base> element may change the address that relative links and form actions resolve against.
    private const string BaseAddress = "base-uri 'none'";

    private const string Shared =
        Fallback + "; " + Scripts + "; " + Styles + "; " + Images + "; " + Forms + "; " + Framing + "; " + BaseAddress;

    private const string Policy = Shared + "; " + Connections;

    private const string DevelopmentPolicy = Shared + "; " + DevelopmentConnections;

    /// <summary>
    /// Adds the policy to every response that the rest of the pipeline writes. Development's policy also lets hot
    /// reload reach the SDK or the IDE; every other environment gets the policy alone.
    /// </summary>
    /// <param name="application">The application's request pipeline.</param>
    /// <param name="environment">The environment the application runs in.</param>
    /// <returns>The pipeline, to add more to.</returns>
    public static IApplicationBuilder UseContentSecurityPolicy(
        this IApplicationBuilder application,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(environment);
        string policy = environment.IsDevelopment() ? DevelopmentPolicy : Policy;
        return application.Use((context, next) =>
        {
            IHeaderDictionary headers = context.Response.Headers;
            headers.ContentSecurityPolicy = policy;
            return next(context);
        });
    }
}
